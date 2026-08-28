using System.Text;
using HyperbolicWarper.Core.Models;

namespace HyperbolicWarper.Core.Parsing;

public static class SrtWriter
{
    public static string ToText(IReadOnlyList<SrtEntry> entries)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            sb.Append(i + 1).Append("\r\n");
            sb.Append(TimeCodeFormat.Format(entry.Start))
              .Append(" --> ")
              .Append(TimeCodeFormat.Format(entry.End))
              .Append("\r\n");

            foreach (var line in entry.Lines)
            {
                sb.Append(line).Append("\r\n");
            }

            sb.Append("\r\n");
        }

        return sb.ToString();
    }

    public static void Write(string path, IReadOnlyList<SrtEntry> entries, Encoding encoding)
    {
        File.WriteAllText(path, ToText(entries), encoding);
    }
}
