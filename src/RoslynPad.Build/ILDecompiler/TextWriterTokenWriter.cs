using System.Globalization;
using System.Text;

namespace RoslynPad.Build.ILDecompiler;

internal static class TextWriterTokenWriter
{
    public static string ConvertString(string str)
    {
        var sb = new StringBuilder();
        foreach (var ch in str)
        {
            sb.Append(ch == '"' ? "\\\"" : ConvertChar(ch));
        }
        return sb.ToString();
    }

    private static string ConvertChar(char ch)
    {
        switch (ch)
        {
            case '\\':
                return "\\\\";
            case '\0':
                return "\\0";
            case '\a':
                return "\\a";
            case '\b':
                return "\\b";
            case '\f':
                return "\\f";
            case '\n':
                return "\\n";
            case '\r':
                return "\\r";
            case '\t':
                return "\\t";
            case '\v':
                return "\\v";
            default:
                if (char.IsControl(ch) || char.IsSurrogate(ch) ||
                    // print all uncommon white spaces as numbers
                    (char.IsWhiteSpace(ch) && ch != ' '))
                {
                    return "\\u" + ((int)ch).ToString("x4", CultureInfo.InvariantCulture);
                }
                else
                {
                    return ch.ToString();
                }
        }
    }
}