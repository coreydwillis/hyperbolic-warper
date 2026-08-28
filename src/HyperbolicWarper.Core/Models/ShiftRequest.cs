namespace HyperbolicWarper.Core.Models;

public enum ShiftMode
{
    /// <summary>Add a fixed (possibly negative) offset to every timecode.</summary>
    Relative,

    /// <summary>Set the first entry's start time to an absolute target; the same delta is applied to every entry.</summary>
    SetFirstStart,
}

public sealed record ShiftRequest
{
    public required ShiftMode Mode { get; init; }

    /// <summary>Signed offset to apply, used when <see cref="Mode"/> is <see cref="ShiftMode.Relative"/>.</summary>
    public TimeSpan RelativeDelta { get; init; }

    /// <summary>Absolute target for the first entry's start time, used when <see cref="Mode"/> is <see cref="ShiftMode.SetFirstStart"/>.</summary>
    public TimeSpan TargetFirstStart { get; init; }

    public static ShiftRequest Relative(TimeSpan delta) =>
        new() { Mode = ShiftMode.Relative, RelativeDelta = delta };

    public static ShiftRequest SetFirstStart(TimeSpan target) =>
        new() { Mode = ShiftMode.SetFirstStart, TargetFirstStart = target };
}
