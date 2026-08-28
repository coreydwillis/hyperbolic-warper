namespace HyperbolicWarper.Core.Models;

public sealed class SrtEntry
{
    public required TimeSpan Start { get; init; }
    public required TimeSpan End { get; init; }
    public required IReadOnlyList<string> Lines { get; init; }
}
