namespace HyperbolicWarper.Core.Models;

/// <summary>Outcome of applying a <see cref="ShiftRequest"/> to one file, used to drive the post-process verification summary.</summary>
public sealed record ShiftResult
{
    public required bool IsEmpty { get; init; }

    public required TimeSpan ExpectedDelta { get; init; }
    public required TimeSpan AppliedDelta { get; init; }

    public required int EntryCountBefore { get; init; }
    public required int EntryCountAfter { get; init; }
    public required int ClampedEntryCount { get; init; }
    public required int OrderingIssueCount { get; init; }

    public required TimeSpan FirstEntryOriginalStart { get; init; }
    public required TimeSpan FirstEntryNewStart { get; init; }
    public required TimeSpan LastEntryOriginalStart { get; init; }
    public required TimeSpan LastEntryNewStart { get; init; }

    public bool DeltaMatchesExpectation => AppliedDelta == ExpectedDelta;

    public bool HasWarnings =>
        !IsEmpty &&
        (ClampedEntryCount > 0
         || OrderingIssueCount > 0
         || EntryCountBefore != EntryCountAfter
         || !DeltaMatchesExpectation);
}
