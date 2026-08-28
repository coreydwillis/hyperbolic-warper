using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
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

        private static IntPtr GetWindowHandle()
        {
            var app = (App)Application.Current;
            return WindowNative.GetWindowHandle(app.MainWindow);
        }

        private void OnSplitterPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            SplitterGrip.Opacity = 1;
        }

        private void OnSplitterPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDraggingSplitter)
            {
                SplitterGrip.Opacity = 0.4;
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
        }

        private void OnSplitterPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            _isDraggingSplitter = false;
        }
    }
}
