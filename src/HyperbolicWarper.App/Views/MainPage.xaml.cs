using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace HyperbolicWarper.App.Views
{
    public partial class MainPage : Page
    {
        private const double MinSettingsColumnWidth = 230;
        private const double MinFileListColumnWidth = 300;

        private bool _isDraggingSplitter;
        private double _dragStartPointerX;
        private double _dragStartColumnWidth;

        public MainViewModel ViewModel { get; } = new();

        public MainPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private async void OnAddFilesClicked(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker, GetWindowHandle());
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".srt");
            picker.ViewMode = PickerViewMode.List;

            var files = await picker.PickMultipleFilesAsync();
            if (files is { Count: > 0 })
            {
                ViewModel.AddFiles(files.Select(f => f.Path));
            }
        }

        private void OnDropZoneDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Add SRT file(s)";
                e.DragUIOverride.IsGlyphVisible = true;
            }
        }

        private async void OnDropZoneDrop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            var paths = items.OfType<StorageFile>().Select(f => f.Path);
            ViewModel.AddFiles(paths);
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void OnHowItWorksClicked(object sender, RoutedEventArgs e)
        {
            // Right padding reserves room for the ScrollViewer's overlay scrollbar so it doesn't
            // sit on top of the text; not capping height lets ContentDialog size it naturally
            // (it already constrains itself to the available window) instead of forcing a scroll
            // that isn't needed.
            var body = new StackPanel { Spacing = 16, Width = 420, Padding = new Thickness(0, 0, 16, 0) };

            body.Children.Add(new TextBlock
            {
                Text = "Shift subtitle timecodes forward or backward, or align the first cue to a new start time, for one file or a whole batch.",
                TextWrapping = TextWrapping.Wrap,
            });

            AddHelpSection(body, "Modes",
                "Shift by amount: enter Hours / Minutes / Seconds / Milliseconds and a direction; every timecode in the file moves by that amount.\n\n" +
                "Set first timecode to: enter the new start time for the first subtitle; the rest of the file follows by the same delta.");

            AddHelpSection(body, "Batch processing",
                "Drag & drop .srt files onto the list, or use File > Open file(s)... \"Apply same shift to all files\" toggles between one shift for everything and giving each file its own controls.");

            AddHelpSection(body, "Output",
                "Overwrite the original file in place, or leave it and write name.shifted.srt next to it.");

            AddHelpSection(body, "Verification",
                "After processing, each file shows a summary. Expand \"Verification details\" for the full before/after breakdown, including any clamped or out-of-order timecodes, and use \"Show in folder\" to locate the result.");

            var dialog = new ContentDialog
            {
                Title = "How Hyperbolic Warper works",
                Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
                CloseButtonText = "Close",
                XamlRoot = XamlRoot,
            };

            await dialog.ShowAsync();
        }

        private static void AddHelpSection(StackPanel container, string heading, string body)
        {
            var section = new StackPanel { Spacing = 4 };
            section.Children.Add(new TextBlock { Text = heading, Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
            section.Children.Add(new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap, Opacity = 0.85 });
            container.Children.Add(section);
        }

        private async void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                .Split('+')[0] ?? "unknown";

            var repoLink = new HyperlinkButton
            {
                NavigateUri = new Uri("https://github.com/coreydwillis/hyperbolic-warper"),
                Content = "github.com/coreydwillis/hyperbolic-warper",
            };

            // ms-appx:// requires package identity, which this unpackaged app doesn't have -- it
            // resolves to nothing and the Image renders blank. Use a real file path instead.
            var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Square150x150Logo.scale-200.png");
            var logo = new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(logoPath)),
                Width = 128,
                Height = 128,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var content = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
            content.Children.Add(logo);
            content.Children.Add(new TextBlock { Text = $"Version {version}", HorizontalAlignment = HorizontalAlignment.Center });
            content.Children.Add(repoLink);
            content.Children.Add(new TextBlock
            {
                Text = "Public domain (The Unlicense) -- no rights reserved",
                Opacity = 0.7,
                HorizontalAlignment = HorizontalAlignment.Center,
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            });

            var dialog = new ContentDialog
            {
                Title = "Hyperbolic Warper",
                Content = content,
                CloseButtonText = "Close",
                XamlRoot = XamlRoot,
            };

            await dialog.ShowAsync();
        }

        private async void OnSaveVerificationLogClicked(object sender, RoutedEventArgs e)
        {
            var processedFiles = ViewModel.Files.Where(f => f.Result is not null || f.Status == BatchFileStatus.Error).ToList();
            if (processedFiles.Count == 0)
            {
                ViewModel.StatusMessage = "No files have been processed yet.";
                return;
            }

            var picker = new FileSavePicker();
            InitializeWithWindow.Initialize(picker, GetWindowHandle());
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.SuggestedFileName = $"HyperbolicWarper-verification-log-{DateTime.Now:yyyy-MM-dd-HHmmss}";
            picker.FileTypeChoices.Add("Text file", new List<string> { ".txt" });

            var file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                return;
            }

            await FileIO.WriteTextAsync(file, BuildVerificationLog(processedFiles));
            ViewModel.LastVerificationLogPath = file.Path;
        }

        private void OnOpenVerificationLogClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel.LastVerificationLogPath is not { } path || !File.Exists(path))
            {
                return;
            }

            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }

        private static string BuildVerificationLog(IReadOnlyList<BatchFileViewModel> files)
        {
            var log = new StringBuilder();
            log.AppendLine("Hyperbolic Warper - Verification Log");
            log.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            log.AppendLine($"Files processed: {files.Count}");
            log.AppendLine();

            foreach (var file in files)
            {
                log.AppendLine(new string('=', 60));
                log.AppendLine(file.FileName);
                log.AppendLine(new string('-', 60));
                log.AppendLine(file.Status == BatchFileStatus.Error
                    ? $"Failed: {file.ErrorMessage}"
                    : file.DetailsText);
                log.AppendLine();
            }

            return log.ToString();
        }

        private static IntPtr GetWindowHandle()
        {
            var app = (App)Application.Current;
            return WindowNative.GetWindowHandle(app.MainWindow);
        }

        private void OnSplitterPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            SplitterGrip.Opacity = 1;
            Splitter.SetCursor(InputSystemCursorShape.SizeWestEast);
        }

        private void OnSplitterPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDraggingSplitter)
            {
                SplitterGrip.Opacity = 0.4;
                Splitter.SetCursor(InputSystemCursorShape.Arrow);
            }
        }

        private void OnSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingSplitter = true;
            _dragStartPointerX = e.GetCurrentPoint(ContentGrid).Position.X;
            _dragStartColumnWidth = SettingsColumn.ActualWidth;
            Splitter.CapturePointer(e.Pointer);
        }

        private void OnSplitterPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDraggingSplitter)
            {
                return;
            }

            var currentX = e.GetCurrentPoint(ContentGrid).Position.X;
            var delta = currentX - _dragStartPointerX;
            var newWidth = _dragStartColumnWidth - delta;

            var maxSettingsWidth = Math.Max(
                MinSettingsColumnWidth,
                ContentGrid.ActualWidth - MinFileListColumnWidth - Splitter.ActualWidth);

            newWidth = Math.Clamp(newWidth, MinSettingsColumnWidth, maxSettingsWidth);
            SettingsColumn.Width = new GridLength(newWidth);
        }

        private void OnSplitterPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingSplitter = false;
            Splitter.ReleasePointerCapture(e.Pointer);
            ResetSplitterCursorIfPointerOutside(e);
        }

        private void OnSplitterPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingSplitter = false;
            ResetSplitterCursorIfPointerOutside(e);
        }

        private void ResetSplitterCursorIfPointerOutside(PointerRoutedEventArgs e)
        {
            var position = e.GetCurrentPoint(Splitter).Position;
            var bounds = new Windows.Foundation.Rect(0, 0, Splitter.ActualWidth, Splitter.ActualHeight);
            if (!bounds.Contains(position))
            {
                SplitterGrip.Opacity = 0.4;
                Splitter.SetCursor(InputSystemCursorShape.Arrow);
            }
        }
    }
}
