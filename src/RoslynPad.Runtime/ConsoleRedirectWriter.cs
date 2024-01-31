using System.Text;

namespace RoslynPad.Runtime;

/// <summary>
/// Redirects the console to the Dump method.
/// </summary>
internal class ConsoleRedirectWriter(JsonConsoleDumper dumper, string? header = null) : TextWriter
{
    private readonly JsonConsoleDumper _dumper = dumper;
    private readonly string? _header = header;

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(string? value)
    {
        if (string.Equals(Environment.NewLine, value, StringComparison.Ordinal))
        {
            return;
        }

        Dump(value);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer != null)
        {
            if (EndsWithNewLine(buffer, index, count))
            {
                count -= Environment.NewLine.Length;
            }

            if (count > 0)
            {
                Dump(new string(buffer, index, count));
            }
        }
    }

    private bool EndsWithNewLine(char[] buffer, int index, int count)
    {
        var nl = Environment.NewLine;

        if (count < nl.Length)
        {
            return false;
        }

        for (int i = nl.Length; i >= 1; --i)
        {
            if (buffer[index + count - i] != nl[nl.Length - i])
            {
                return false;
            }
        }

        return true;
    }

    public override void Write(char value) => Dump(value);

    private void Dump(object? value) => _dumper.Dump(new DumpData(value, _header, Line: null, DumpQuotas.Default));
}
