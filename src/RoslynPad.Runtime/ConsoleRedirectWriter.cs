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
        if (value == null)
        {
            return;
        }

        var endsLine = value.Length > 0 && value[value.Length - 1] == '\n';
        if (endsLine)
        {
            var length = value.Length - 1;
            if (length > 0 && value[length - 1] == '\r')
            {
                length--;
            }

            value = value.Substring(0, length);
        }

        Dump(value, endsLine);
    }

    public override void WriteLine(string? value) => Dump(value ?? string.Empty, endsLine: true);

    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer == null)
        {
            return;
        }

        var endsLine = count > 0 && buffer[index + count - 1] == '\n';
        if (endsLine)
        {
            count--;
            if (count > 0 && buffer[index + count - 1] == '\r')
            {
                count--;
            }
        }

        if (count > 0 || endsLine)
        {
            Dump(count > 0 ? new string(buffer, index, count) : string.Empty, endsLine);
        }
    }

    public override void Write(bool value) => Dump(value);

    public override void Write(char value)
    {
        if (value == '\n')
        {
            Dump(string.Empty, endsLine: true);
        }
        else
        {
            Dump(value);
        }
    }

    public override void Write(decimal value) => Dump(value);

    public override void Write(double value) => Dump(value);

    public override void Write(float value) => Dump(value);

    public override void Write(int value) => Dump(value);

    public override void Write(long value) => Dump(value);

    public override void Write(object? value) => Dump(value);

    public override void Write(uint value) => Dump(value);

    public override void Write(ulong value) => Dump(value);

    public override void WriteLine(object? value) => Dump(value, endsLine: true);

    private void Dump(object? value, bool endsLine = false) =>
        _dumper.Dump(new DumpData(value, _header, Line: null, DumpQuotas.Default, endsLine));
}
