using System.Text.RegularExpressions;

namespace HyperbolicWarper.Core.Parsing;

/// <summary>Parses and formats SRT timecodes ("HH:MM:SS,mmm"), tolerant of "." instead of "," and short digit groups.</summary>
public static partial class TimeCodeFormat
{
    [GeneratedRegex(@"^(\d+):(\d{1,2}):(\d{1,2})[,.](\d{1,3})$")]
    private static partial Regex TimeCodePattern();

    public static TimeSpan Parse(string value)
    {
        var trimmed = value.Trim();
        var match = TimeCodePattern().Match(trimmed);
        if (!match.Success)
        {
            throw new SrtParseException($"Invalid timecode '{value}'.");
        }

        var hours = int.Parse(match.Groups[1].Value);
        var minutes = int.Parse(match.Groups[2].Value);
        var seconds = int.Parse(match.Groups[3].Value);
        var milliseconds = int.Parse(match.Groups[4].Value.PadRight(3, '0'));

        return new TimeSpan(0, hours, minutes, seconds, milliseconds);
    }

    /// <summary>Parses a "start --> end" line, ignoring any trailing cue-settings tokens after the end timecode.</summary>
    public static (TimeSpan Start, TimeSpan End) ParseRange(string timingLine)
    {
        var arrowIndex = timingLine.IndexOf("-->", StringComparison.Ordinal);
        if (arrowIndex < 0)
        {
            throw new SrtParseException($"Invalid timing line '{timingLine}'.");
        }

        var startText = timingLine[..arrowIndex];
        var afterArrow = timingLine[(arrowIndex + 3)..].Trim();
        var endText = afterArrow.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        return (Parse(startText), Parse(endText));
    }

    public static string Format(TimeSpan value)
    {
        var clamped = value < TimeSpan.Zero ? TimeSpan.Zero : value;
        var totalHours = (long)clamped.TotalHours;
        return $"{totalHours:00}:{clamped.Minutes:00}:{clamped.Seconds:00},{clamped.Milliseconds:000}";
    }
}
