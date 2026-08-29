using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Dispatching;
using HyperbolicWarper.Core.Services;

namespace HyperbolicWarper.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<BatchFileViewModel> Files { get; } = new();

    [ObservableProperty]
    private ShiftSettingsViewModel _globalShift = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PerFileShiftVisible))]
    private bool _applySameShiftToAllFiles = true;

    [ObservableProperty]
    private bool _overwriteOriginalFiles;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessAllCommand))]
    private bool _isProcessing;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string? _validationMessage;

    public bool HasValidationMessage => ValidationMessage is not null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProcessAllCommand))]
    private bool _hasFiles;

    [ObservableProperty]
    private bool _hasProcessedFiles;

    [ObservableProperty]
    private string? _lastVerificationLogPath;

    public bool PerFileShiftVisible => !ApplySameShiftToAllFiles;

    public MainViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Files.CollectionChanged += OnFilesCollectionChanged;
        GlobalShift.PropertyChanged += OnGlobalShiftPropertyChanged;
    }

    private void OnGlobalShiftPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Relative vs. Set-First-Start is treated as one workflow choice for the whole batch,
        // even when each file has its own H/M/S/Ms amount, so keep every row's mode in sync.
        if (e.PropertyName == nameof(ShiftSettingsViewModel.Mode))
        {
            foreach (var file in Files)
            {
                file.Shift.Mode = GlobalShift.Mode;
            }
        }
    }

    private void OnFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasFiles = Files.Count > 0;
        RefreshHasProcessedFiles();
    }

    private void RefreshHasProcessedFiles() =>
        HasProcessedFiles = Files.Any(f => f.Result is not null || f.Status == BatchFileStatus.Error);

    public void AddFiles(IEnumerable<string> paths)
    {
        var added = 0;
        var skipped = 0;

        foreach (var path in paths)
        {
            if (!path.EndsWith(".srt", StringComparison.OrdinalIgnoreCase))
            {
                skipped++;
                continue;
            }

            if (Files.Any(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var file = new BatchFileViewModel(path);
            file.Shift.Mode = GlobalShift.Mode;
            Files.Add(file);
            added++;
        }

        ValidationMessage = skipped > 0
            ? $"Skipped {skipped} file(s) that were not .srt subtitle files."
            : null;

        if (added > 0)
        {
            StatusMessage = $"{Files.Count} file(s) queued.";
        }
    }

    [RelayCommand]
    private void RemoveFile(BatchFileViewModel file) => Files.Remove(file);

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
        StatusMessage = null;
        LastVerificationLogPath = null;
    }

    private bool CanProcessAll() => HasFiles && !IsProcessing;

    [RelayCommand(CanExecute = nameof(CanProcessAll))]
    private async Task ProcessAllAsync(CancellationToken cancellationToken)
    {
        IsProcessing = true;
        OverallProgress = 0;
        ValidationMessage = null;
        LastVerificationLogPath = null;

        foreach (var file in Files)
        {
            file.Reset();
        }

        var total = Files.Count;
        var completed = 0;

        try
        {
            await Parallel.ForEachAsync(
                Files,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount),
                    CancellationToken = cancellationToken,
                },
                async (file, token) =>
                {
                    _dispatcherQueue.TryEnqueue(() => file.Status = BatchFileStatus.Processing);

                    var shiftSettings = ApplySameShiftToAllFiles ? GlobalShift : file.Shift;
                    var options = new ProcessFileOptions
                    {
                        Shift = shiftSettings.ToShiftRequest(),
                        Overwrite = OverwriteOriginalFiles,
                    };

                    var outcome = await Task.Run(() => SrtFileProcessor.Process(file.Path, options), token);

                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        file.ApplyOutcome(outcome);
                        completed++;
                        OverallProgress = total == 0 ? 0 : (double)completed / total * 100;
                    });
                });

            var errorCount = Files.Count(f => f.Status == BatchFileStatus.Error);
            var warningCount = Files.Count(f => f.Status == BatchFileStatus.Warning);
            StatusMessage = errorCount == 0 && warningCount == 0
                ? $"Processed {total} file(s) successfully."
                : $"Processed {total} file(s): {warningCount} with warnings, {errorCount} failed.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Processing cancelled.";
        }
        finally
        {
            IsProcessing = false;
            RefreshHasProcessedFiles();
        }
    }
}
