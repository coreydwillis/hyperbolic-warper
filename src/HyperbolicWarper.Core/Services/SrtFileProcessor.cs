using HyperbolicWarper.Core.Models;
using HyperbolicWarper.Core.Parsing;

namespace HyperbolicWarper.Core.Services;

public sealed record ProcessFileOptions
{
    public required ShiftRequest Shift { get; init; }
    public required bool Overwrite { get; init; }
}

public sealed record ProcessFileOutcome
{
    public required string SourcePath { get; init; }
    public string? OutputPath { get; init; }
    public ShiftResult? Result { get; init; }
    public string? Error { get; init; }

    public bool Succeeded => Error is null;
}

/// <summary>Orchestrates parse -> shift -> write for a single file. Pure and UI-free so it can run off the UI thread and be unit tested directly.</summary>
public static class SrtFileProcessor
{
    public static ProcessFileOutcome Process(string path, ProcessFileOptions options)
    {
        try
        {
            var document = SrtParser.Parse(path);
            var (shiftedDocument, result) = TimeShiftService.Apply(document, options.Shift);

            var outputPath = options.Overwrite ? path : BuildSuffixedPath(path);
            SrtWriter.Write(outputPath, shiftedDocument.Entries, shiftedDocument.Encoding);

            return new ProcessFileOutcome
            {
                SourcePath = path,
                OutputPath = outputPath,
                Result = result,
            };
        }
        catch (SrtParseException ex)
        {
            return new ProcessFileOutcome { SourcePath = path, Error = ex.Message };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ProcessFileOutcome { SourcePath = path, Error = ex.Message };
        }
    }

    public static string BuildSuffixedPath(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        return Path.Combine(directory, $"{name}.shifted{extension}");
    }
}
