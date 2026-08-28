using System.Globalization;
using HyperbolicWarper.Core.Models;
using HyperbolicWarper.Core.Services;

namespace HyperbolicWarper.App.ViewModels;

public enum BatchFileStatus
{
    Queued,
    Processing,
    Done,
    Warning,
    Error,
}

public partial class BatchFileViewModel : ObservableObject
{
    public string Path { get; }

    public string FileName => System.IO.Path.GetFileName(Path);

    [ObservableProperty]
    private BatchFileStatus _status = BatchFileStatus.Queued;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    [NotifyPropertyChangedFor(nameof(DetailsText))]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryText))]
    [NotifyPropertyChangedFor(nameof(DetailsText))]
    private ShiftResult? _result;

    [ObservableProperty]
    private string? _outputPath;

    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>Per-file shift override, used when the batch is not sharing a single global shift.</summary>
    public ShiftSettingsViewModel Shift { get; } = new();

    public BatchFileViewModel(string path)
    {
        Path = path;
    }

    public void Reset()
    {
        Status = BatchFileStatus.Queued;
        ErrorMessage = null;
        Result = null;
        OutputPath = null;
    }

    public void ApplyOutcome(ProcessFileOutcome outcome)
    {
        OutputPath = outcome.OutputPath;
        Result = outcome.Result;

        if (!outcome.Succeeded)
        {
            Status = BatchFileStatus.Error;
            ErrorMessage = outcome.Error;
        }
        else if (outcome.Result is { HasWarnings: true })
        {
            Status = BatchFileStatus.Warning;
        }
        else
        {
            Status = BatchFileStatus.Done;
        }
    }

    // Segoe Fluent Icons glyphs: More(pending), Sync, CheckMark, Warning, Cancel.
    public string StatusGlyph => Status switch
    {
        BatchFileStatus.Queued => "",
        BatchFileStatus.Processing => "",
        BatchFileStatus.Done => "",
        BatchFileStatus.Warning => "",
        BatchFileStatus.Error => "",
        _ => "",
    };

    public string SummaryText
    {
        get
        {
            if (Status == BatchFileStatus.Error)
            {
                return ErrorMessage ?? "Failed to process file.";
            }

            if (Result is null)
            {
                return "Waiting to process.";
            }

            if (Result.IsEmpty)
            {
                return "File has no subtitle entries.";
            }

            var sign = Result.AppliedDelta < TimeSpan.Zero ? "-" : "+";
            var deltaText = $"{sign}{FormatSpan(Result.AppliedDelta.Duration())}";
            var countText = $"{Result.EntryCountAfter} entries";

            if (!Result.HasWarnings)
            {
                return $"Shifted {deltaText}, {countText}, no issues.";
            }

            var warnings = new List<string>();
            if (Result.ClampedEntryCount > 0)
            {
                warnings.Add($"{Result.ClampedEntryCount} timecode(s) clamped to 00:00:00,000");
            }
            if (Result.OrderingIssueCount > 0)
            {
                warnings.Add($"{Result.OrderingIssueCount} ordering issue(s)");
            }
            if (Result.EntryCountBefore != Result.EntryCountAfter)
            {
                warnings.Add("entry count changed");
            }
            if (!Result.DeltaMatchesExpectation)
            {
                warnings.Add("applied delta differs from requested delta");
            }

            return $"Shifted {deltaText}, {countText}: " + string.Join(", ", warnings);
        }
    }

    public string DetailsText
    {
        get
        {
            if (Result is null || Result.IsEmpty)
            {
                return string.Empty;
            }

            return string.Join(
                Environment.NewLine,
                $"Requested shift: {FormatSignedSpan(Result.ExpectedDelta)}",
                $"Applied shift: {FormatSignedSpan(Result.AppliedDelta)}",
                $"First entry: {FormatTimestamp(Result.FirstEntryOriginalStart)} → {FormatTimestamp(Result.FirstEntryNewStart)}",
                $"Last entry: {FormatTimestamp(Result.LastEntryOriginalStart)} → {FormatTimestamp(Result.LastEntryNewStart)}",
                $"Entries: {Result.EntryCountBefore} → {Result.EntryCountAfter}",
                $"Clamped to zero: {Result.ClampedEntryCount}",
                $"Ordering issues: {Result.OrderingIssueCount}",
                $"Output: {OutputPath}");
        }
    }

    private static string FormatSignedSpan(TimeSpan span)
    {
        var sign = span < TimeSpan.Zero ? "-" : "+";
        return sign + FormatSpan(span.Duration());
    }

    private static string FormatSpan(TimeSpan span) =>
        span.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

    private static string FormatTimestamp(TimeSpan span) =>
        span.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
}
