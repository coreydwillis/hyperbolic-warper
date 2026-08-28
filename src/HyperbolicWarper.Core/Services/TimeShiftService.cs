using HyperbolicWarper.Core.Models;

namespace HyperbolicWarper.Core.Services;

public static class TimeShiftService
{
    public static (SrtDocument Document, ShiftResult Result) Apply(SrtDocument source, ShiftRequest request)
    {
        if (source.Entries.Count == 0)
        {
            return (source, EmptyResult());
        }

        var entries = source.Entries;
        var expectedDelta = request.Mode == ShiftMode.Relative
            ? request.RelativeDelta
            : request.TargetFirstStart - entries[0].Start;

        var clampedCount = 0;
        var shifted = new List<SrtEntry>(entries.Count);

        foreach (var entry in entries)
        {
            var (newStart, startClamped) = ShiftAndClamp(entry.Start, expectedDelta);
            var (newEnd, endClamped) = ShiftAndClamp(entry.End, expectedDelta);
            if (startClamped) clampedCount++;
            if (endClamped) clampedCount++;

            shifted.Add(new SrtEntry { Start = newStart, End = newEnd, Lines = entry.Lines });
        }

        var orderingIssues = 0;
        for (var i = 0; i < shifted.Count; i++)
        {
            if (shifted[i].End < shifted[i].Start) orderingIssues++;
            if (i > 0 && shifted[i].Start < shifted[i - 1].Start) orderingIssues++;
        }

        var result = new ShiftResult
        {
            IsEmpty = false,
            ExpectedDelta = expectedDelta,
            AppliedDelta = shifted[0].Start - entries[0].Start,
            EntryCountBefore = entries.Count,
            EntryCountAfter = shifted.Count,
            ClampedEntryCount = clampedCount,
            OrderingIssueCount = orderingIssues,
            FirstEntryOriginalStart = entries[0].Start,
            FirstEntryNewStart = shifted[0].Start,
            LastEntryOriginalStart = entries[^1].Start,
            LastEntryNewStart = shifted[^1].Start,
        };

        return (new SrtDocument { Entries = shifted, Encoding = source.Encoding }, result);
    }

    private static (TimeSpan Value, bool Clamped) ShiftAndClamp(TimeSpan original, TimeSpan delta)
    {
        var shifted = original + delta;
        return shifted < TimeSpan.Zero ? (TimeSpan.Zero, true) : (shifted, false);
    }

    private static ShiftResult EmptyResult() => new()
    {
        IsEmpty = true,
        ExpectedDelta = TimeSpan.Zero,
        AppliedDelta = TimeSpan.Zero,
        EntryCountBefore = 0,
        EntryCountAfter = 0,
        ClampedEntryCount = 0,
        OrderingIssueCount = 0,
        FirstEntryOriginalStart = TimeSpan.Zero,
        FirstEntryNewStart = TimeSpan.Zero,
        LastEntryOriginalStart = TimeSpan.Zero,
        LastEntryNewStart = TimeSpan.Zero,
    };
}
