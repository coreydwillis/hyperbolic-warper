using HyperbolicWarper.Core.Models;

namespace HyperbolicWarper.App.ViewModels;

/// <summary>
/// Backs one shift editor (the global one, or a per-file row when batch files use individual shifts).
/// The same Hours/Minutes/Seconds/Milliseconds fields represent either a signed offset (Relative mode)
/// or an absolute target for the first entry's start time (SetFirstStart mode).
/// </summary>
public partial class ShiftSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private ShiftMode _mode = ShiftMode.Relative;

    [ObservableProperty]
    private double _hours;

    [ObservableProperty]
    private double _minutes;

    [ObservableProperty]
    private double _seconds;

    [ObservableProperty]
    private double _milliseconds;

    [ObservableProperty]
    private bool _isForward = true;

    public ShiftRequest ToShiftRequest()
    {
        var magnitude = new TimeSpan(0, 0, 0, 0)
            + TimeSpan.FromHours((int)Hours)
            + TimeSpan.FromMinutes((int)Minutes)
            + TimeSpan.FromSeconds((int)Seconds)
            + TimeSpan.FromMilliseconds((int)Milliseconds);

        if (Mode == ShiftMode.Relative)
        {
            var delta = IsForward ? magnitude : magnitude.Negate();
            return ShiftRequest.Relative(delta);
        }

        return ShiftRequest.SetFirstStart(magnitude);
    }

    public void CopyFrom(ShiftSettingsViewModel other)
    {
        Mode = other.Mode;
        Hours = other.Hours;
        Minutes = other.Minutes;
        Seconds = other.Seconds;
        Milliseconds = other.Milliseconds;
        IsForward = other.IsForward;
    }
}
