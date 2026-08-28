using HyperbolicWarper.Core.Models;

namespace HyperbolicWarper.Core.Parsing;

public static class SrtParser
{
    public static SrtDocument Parse(string path)
    {
        var (text, encoding) = SrtFileEncoding.ReadAllText(path);
        return new SrtDocument { Entries = ParseText(text), Encoding = encoding };
    }

    public static IReadOnlyList<SrtEntry> ParseText(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var blocks = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        var entries = new List<SrtEntry>();
        var cueNumber = 0;

        foreach (var rawBlock in blocks)
        {
            // A trailing blank line after the last cue (common in files with an uploader
            // "credits" cue at the end) leaves a whitespace-only block here: "\n\n\n" splits
            // on "\n\n" into the last real cue plus a leftover "\n". RemoveEmptyEntries only
            // strips truly-empty "" blocks, not whitespace-only ones, so skip those too.
            if (string.IsNullOrWhiteSpace(rawBlock))
            {
                continue;
            }

            cueNumber++;
            var lines = rawBlock.Split('\n');
            entries.Add(ParseBlock(lines, cueNumber));
        }

        return entries;
    }

    private static SrtEntry ParseBlock(string[] lines, int cueNumber)
    {
        if (lines.Length == 0)
        {
            throw new SrtParseException($"Cue #{cueNumber} is empty.");
        }

        // Normally lines[0] is the numeric index and lines[1] is the timing line, but tolerate
        // a missing/blank index line by detecting the timing line directly.
        var timingLineIndex = lines[0].Contains("-->", StringComparison.Ordinal) ? 0 : 1;
        if (timingLineIndex >= lines.Length)
        {
            throw new SrtParseException($"Cue #{cueNumber} is missing a timing line.");
        }

        TimeSpan start, end;
        try
        {
            (start, end) = TimeCodeFormat.ParseRange(lines[timingLineIndex]);
        }
        catch (SrtParseException ex)
        {
            throw new SrtParseException($"Cue #{cueNumber}: {ex.Message}");
        }

        var textLines = lines.Skip(timingLineIndex + 1).ToList();

        return new SrtEntry { Start = start, End = end, Lines = textLines };
    }
}
