namespace HyperbolicWarper.App.Views;

public sealed partial class ShiftInputControl : UserControl
{
    public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
        nameof(Settings),
        typeof(ShiftSettingsViewModel),
        typeof(ShiftInputControl),
        new PropertyMetadata(null, OnSettingsChanged));

    public ShiftSettingsViewModel? Settings
    {
        get => (ShiftSettingsViewModel?)GetValue(SettingsProperty);
        set => SetValue(SettingsProperty, value);
    }

    public ShiftInputControl()
    {
        InitializeComponent();
    }

    private static void OnSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShiftInputControl { RootPanel: not null } control)
        {
            control.RootPanel.DataContext = e.NewValue;
        }
    }
}
