using System.Text;
using HyperbolicWarper.Core.Models;
using HyperbolicWarper.Core.Services;
using Xunit;

namespace HyperbolicWarper.Core.Tests;

public class TimeShiftServiceTests
{
    private static SrtDocument BuildDocument(params (int startMs, int endMs)[] entries)
    {
        var list = entries
            .Select(e => new SrtEntry
            {
                Start = TimeSpan.FromMilliseconds(e.startMs),
                End = TimeSpan.FromMilliseconds(e.endMs),
                Lines = ["text"],
            })
            .ToList();

        return new SrtDocument { Entries = list, Encoding = Encoding.UTF8 };
    }

    [Fact]
    public void Apply_RelativePositiveShift_AddsDeltaToEveryEntry()
    {
        var document = BuildDocument((1000, 2000), (5000, 6000));
        var request = ShiftRequest.Relative(TimeSpan.FromSeconds(5));

        var (shifted, result) = TimeShiftService.Apply(document, request);

        Assert.Equal(TimeSpan.FromMilliseconds(6000), shifted.Entries[0].Start);
        Assert.Equal(TimeSpan.FromMilliseconds(11000), shifted.Entries[1].End);
        Assert.Equal(TimeSpan.FromSeconds(5), result.AppliedDelta);
        Assert.True(result.DeltaMatchesExpectation);
        Assert.Equal(0, result.ClampedEntryCount);
        Assert.False(result.HasWarnings);
    }

    [Fact]
    public void Apply_RelativeNegativeShiftBeyondZero_ClampsToZeroNotNegative()
    {
        var document = BuildDocument((1000, 2000), (5000, 6000));
        var request = ShiftRequest.Relative(TimeSpan.FromSeconds(-3));

        var (shifted, result) = TimeShiftService.Apply(document, request);

        Assert.Equal(TimeSpan.Zero, shifted.Entries[0].Start);
        Assert.True(shifted.Entries[0].End >= TimeSpan.Zero);
        Assert.Equal(2, result.ClampedEntryCount); // entry[0].Start (1000-3000) and entry[0].End (2000-3000) both go negative and clamp
        Assert.True(result.HasWarnings);
    }

    [Fact]
    public void Apply_SetFirstStart_ComputesDeltaFromFirstEntryAndAppliesUniformly()
    {
        var document = BuildDocument((1416, 3000), (5000, 6000));
        var target = TimeSpan.FromMilliseconds(8360);
        var request = ShiftRequest.SetFirstStart(target);

        var (shifted, result) = TimeShiftService.Apply(document, request);

        Assert.Equal(target, shifted.Entries[0].Start);
        var expectedDelta = target - TimeSpan.FromMilliseconds(1416);
        Assert.Equal(expectedDelta, result.ExpectedDelta);
        Assert.Equal(expectedDelta, result.AppliedDelta);
        Assert.Equal(TimeSpan.FromMilliseconds(5000) + expectedDelta, shifted.Entries[1].Start);
    }

    [Fact]
    public void Apply_PreservesEntryCountAndText()
    {
        var document = BuildDocument((0, 1000), (2000, 3000), (4000, 5000));

        var (shifted, result) = TimeShiftService.Apply(document, ShiftRequest.Relative(TimeSpan.FromSeconds(1)));

        Assert.Equal(3, result.EntryCountBefore);
        Assert.Equal(3, result.EntryCountAfter);
        Assert.All(shifted.Entries, e => Assert.Equal(["text"], e.Lines));
    }

    [Fact]
    public void Apply_EmptyDocument_ReturnsEmptyResultWithoutThrowing()
    {
        var document = new SrtDocument { Entries = [], Encoding = Encoding.UTF8 };

        var (shifted, result) = TimeShiftService.Apply(document, ShiftRequest.Relative(TimeSpan.FromSeconds(5)));

        Assert.True(result.IsEmpty);
        Assert.Empty(shifted.Entries);
        Assert.False(result.HasWarnings);
    }
}
