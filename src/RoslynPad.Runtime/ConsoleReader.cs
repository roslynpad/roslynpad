using System;
using System.IO;

namespace RoslynPad.Runtime;

internal class ConsoleReader : TextReader
{
    private readonly TextReader _reader;
    private readonly JsonConsoleDumper _dumper;

    private string? _readString;
    private int _readPosition;

    public ConsoleReader(JsonConsoleDumper dumper)
    {
        _dumper = dumper;
        _reader = new StreamReader(Console.OpenStandardInput());
    }

    public override int Read()
    {
        if (_readString == null || _readPosition >= _readString.Length - 1)
        {
            _dumper.DumpInputReadRequest();

            _readString = _reader.ReadLine() + Environment.NewLine;
            _readPosition = 0;
        }

        return _readString[_readPosition++];
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _reader.Dispose();
        }
    }
}
