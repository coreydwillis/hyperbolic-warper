using HyperbolicWarper.Core.Models;
using HyperbolicWarper.Core.Services;
using Xunit;

namespace HyperbolicWarper.Core.Tests;

public class SrtFileProcessorTests
{
    private const string Content =
        "1\r\n00:00:01,000 --> 00:00:02,000\r\nHello\r\n\r\n" +
        "2\r\n00:00:03,000 --> 00:00:04,000\r\nWorld\r\n\r\n";

    [Fact]
    public void Process_WithoutOverwrite_WritesSuffixedFileAndLeavesOriginalUntouched()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "episode.srt");
            File.WriteAllText(path, Content);

            var outcome = SrtFileProcessor.Process(path, new ProcessFileOptions
            {
                Shift = ShiftRequest.Relative(TimeSpan.FromSeconds(5)),
                Overwrite = false,
            });

            Assert.True(outcome.Succeeded);
            var expectedOutput = Path.Combine(dir.FullName, "episode.shifted.srt");
            Assert.Equal(expectedOutput, outcome.OutputPath);
            Assert.True(File.Exists(expectedOutput));
            Assert.Contains("00:00:06,000", File.ReadAllText(expectedOutput));
            Assert.Contains("00:00:01,000", File.ReadAllText(path)); // original untouched
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Process_WithOverwrite_ReplacesOriginalFile()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "episode.srt");
            File.WriteAllText(path, Content);

            var outcome = SrtFileProcessor.Process(path, new ProcessFileOptions
            {
                Shift = ShiftRequest.Relative(TimeSpan.FromSeconds(5)),
                Overwrite = true,
            });

            Assert.True(outcome.Succeeded);
            Assert.Equal(path, outcome.OutputPath);
            Assert.Contains("00:00:06,000", File.ReadAllText(path));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Process_MalformedFile_ReturnsErrorInsteadOfThrowing()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "broken.srt");
            File.WriteAllText(path, "not a valid srt file at all");

            var outcome = SrtFileProcessor.Process(path, new ProcessFileOptions
            {
                Shift = ShiftRequest.Relative(TimeSpan.FromSeconds(1)),
                Overwrite = false,
            });

            Assert.False(outcome.Succeeded);
            Assert.NotNull(outcome.Error);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
