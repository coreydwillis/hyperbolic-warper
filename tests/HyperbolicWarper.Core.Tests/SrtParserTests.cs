using System.Text;
using HyperbolicWarper.Core.Parsing;
using Xunit;

namespace HyperbolicWarper.Core.Tests;

public class SrtParserTests
{
    private const string SampleCrLf =
        "1\r\n00:00:06,041 --> 00:00:09,665\r\nThis is it, for better or worse.\r\n\r\n" +
        "2\r\n00:00:10,458 --> 00:00:12,957\r\nWho knows? One week.\r\n\r\n" +
        "3\r\n00:00:13,583 --> 00:00:16,874\r\nOne teeny-weeny week, my boy.\r\nSecond line of text.\r\n\r\n";

    [Fact]
    public void ParseText_ParsesAllCuesInOrder()
    {
        var entries = SrtParser.ParseText(SampleCrLf);

        Assert.Equal(3, entries.Count);
        Assert.Equal(new TimeSpan(0, 0, 0, 6, 41), entries[0].Start);
        Assert.Equal(new TimeSpan(0, 0, 0, 9, 665), entries[0].End);
        Assert.Equal(["This is it, for better or worse."], entries[0].Lines);
        Assert.Equal(["One teeny-weeny week, my boy.", "Second line of text."], entries[2].Lines);
    }

    [Fact]
    public void ParseText_TreatsLfOnlyLineEndingsTheSameAsCrLf()
    {
        var lfOnly = SampleCrLf.Replace("\r\n", "\n");

        var entries = SrtParser.ParseText(lfOnly);

        Assert.Equal(3, entries.Count);
        Assert.Equal(new TimeSpan(0, 0, 0, 6, 41), entries[0].Start);
    }

    [Fact]
    public void ParseText_ThrowsWithCueNumberWhenTimingLineIsMissing()
    {
        const string malformed = "1\r\nNot a timing line\r\nSome text\r\n\r\n";

        var ex = Assert.Throws<SrtParseException>(() => SrtParser.ParseText(malformed));
        Assert.Contains("#1", ex.Message);
    }

    [Fact]
    public void Parse_DetectsUtf8Bom()
    {
        var path = Path.GetTempFileName();
        try
        {
            var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            File.WriteAllText(path, SampleCrLf, utf8Bom);

            var document = SrtParser.Parse(path);

            Assert.Equal(3, document.Entries.Count);
            Assert.True(document.Encoding.GetPreamble().Length > 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RoundTrip_ParseThenWrite_RenumbersSequentiallyAndPreservesTimingAndText()
    {
        var entries = SrtParser.ParseText(SampleCrLf);

        var text = SrtWriter.ToText(entries);
        var reparsed = SrtParser.ParseText(text);

        Assert.Equal(entries.Count, reparsed.Count);
        for (var i = 0; i < entries.Count; i++)
        {
            Assert.Equal(entries[i].Start, reparsed[i].Start);
            Assert.Equal(entries[i].End, reparsed[i].End);
            Assert.Equal(entries[i].Lines, reparsed[i].Lines);
        }
        Assert.StartsWith("1\r\n", text);
    }
}
