using System.Text;

namespace HyperbolicWarper.Core.Parsing;

/// <summary>
/// Detects the text encoding of a subtitle file from its byte-order mark, falling back to
/// strict UTF-8 and then legacy Windows-1252 for files saved without a BOM (common for older SRT files).
/// </summary>
public static class SrtFileEncoding
{
    public static (string Text, Encoding Encoding) Decode(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            return (Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), encoding);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), Encoding.Unicode);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return (Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), Encoding.BigEndianUnicode);
        }

        var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            return (strictUtf8.GetString(bytes), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (DecoderFallbackException)
        {
            var legacy = Encoding.GetEncoding("windows-1252");
            return (legacy.GetString(bytes), legacy);
        }
    }

    public static (string Text, Encoding Encoding) ReadAllText(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Decode(bytes);
    }
}
