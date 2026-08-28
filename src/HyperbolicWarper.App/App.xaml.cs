using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Graphics;

namespace HyperbolicWarper.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        public Window MainWindow => _window ?? throw new InvalidOperationException("Window not yet created.");

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            _window = new Window
            {
                Title = "Hyperbolic Warper",
                SystemBackdrop = new MicaBackdrop(),
            };

            var rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            _window.Content = rootFrame;

            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            _window.Activate();

            // Resizing before Activate() can pick up the wrong DPI on some displays, so size the window afterward.
            if (_window.AppWindow is { } appWindow)
            {
                appWindow.Resize(new SizeInt32(1180, 1150));
                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.PreferredMinimumWidth = 700;
                    presenter.PreferredMinimumHeight = 480;
                }
            }
        }

        private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
