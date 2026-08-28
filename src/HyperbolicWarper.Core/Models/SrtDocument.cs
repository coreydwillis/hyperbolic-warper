using System.Text;

namespace HyperbolicWarper.Core.Models;

public sealed class SrtDocument
{
    public required IReadOnlyList<SrtEntry> Entries { get; init; }

    public required Encoding Encoding { get; init; }
}
