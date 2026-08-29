using System.Reflection;
using System.Runtime.InteropServices;
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

            var logo = new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png")),
                Width = 96,
                Height = 96,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var content = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
            content.Children.Add(logo);
            content.Children.Add(new TextBlock { Text = $"Version {version}", HorizontalAlignment = HorizontalAlignment.Center });
            content.Children.Add(repoLink);

            var dialog = new ContentDialog
            {
                Title = "Hyperbolic Warper",
                Content = content,
                CloseButtonText = "Close",
                XamlRoot = XamlRoot,
            };

            await dialog.ShowAsync();
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
