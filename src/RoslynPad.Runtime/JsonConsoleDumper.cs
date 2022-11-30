using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Xml;

namespace RoslynPad.Runtime;

internal class JsonConsoleDumper : IConsoleDumper, IDisposable
{
    private const int MaxDumpsPerSession = 100000;

    private static readonly byte[] s_newLine = Encoding.UTF8.GetBytes(Environment.NewLine);

    private static readonly byte[] s_resultObjectHeader = Encoding.UTF8.GetBytes("o:");
    private static readonly byte[] s_exceptionResultHeader = Encoding.UTF8.GetBytes("e:");
    private static readonly byte[] s_inputReadRequestHeader = Encoding.UTF8.GetBytes("i:");
    private static readonly byte[] s_progressResultHeader = Encoding.UTF8.GetBytes("p:");

    private readonly Stream _stream;

    private readonly object _lock;

    private int _dumpCount;

    public JsonConsoleDumper()
    {
        _stream = Console.OpenStandardOutput();

        _lock = new object();
    }

    // this assembly shouldn't have any external dependencies, so using this legacy JSON writer
    private XmlDictionaryWriter CreateJsonWriter() =>
        JsonReaderWriterFactory.CreateJsonWriter(_stream, Encoding.UTF8, ownsStream: false);

    public bool SupportsRedirect => true;

    public TextWriter CreateWriter(string? header = null) => new ConsoleRedirectWriter(this, header);

    public TextReader CreateReader() => new ConsoleReader(this);

    public void Dispose() => _stream.Dispose();

    public void Dump(in DumpData data)
    {
        if (!CanDump())
        {
            return;
        }

        try
        {
            DumpResultObject(ResultObject.Create(data.Object, data.Quotas, data.Header));
        }
        catch (Exception ex)
        {
            try
            {
                DumpMessage("Error during Dump: " + ex.Message);
            }
            catch
            {
                // ignore
            }
        }
    }

    public void DumpException(Exception exception)
    {
        if (!CanDump())
        {
            return;
        }

        try
        {
            DumpExceptionResultObject(ExceptionResultObject.Create(exception));
        }
        catch (Exception ex)
        {
            try
            {
                DumpMessage("Error during Dump: " + ex.Message);
            }
            catch
            {
                // ignore
            }
        }
    }

    public void DumpProgress(ProgressResultObject result)
    {
        lock (_lock)
        {
            Write(s_progressResultHeader);

            using (var jsonWriter = CreateJsonWriter())
            {
                using var _ = jsonWriter.WriteObject();
                if (result.Progress != null)
                {
                    jsonWriter.WriteProperty("p", result.Progress.Value);
                }
            }

            WriteNewLine();
        }
    }

    public void Flush() => _stream.Flush();

    private bool CanDump()
    {
        var currentCount = Interlocked.Increment(ref _dumpCount);
        if (currentCount >= MaxDumpsPerSession)
        {
            if (currentCount == MaxDumpsPerSession)
            {
                DumpMessage("<max results reached>");
            }

            return false;
        }

        return true;
    }

    private void DumpMessage(string message)
    {
        lock (_lock)
        {
            Write(s_resultObjectHeader);

            using (var jsonWriter = CreateJsonWriter())
            {
                using var _ = jsonWriter.WriteObject();
                jsonWriter.WriteProperty("v", message);
            }

            WriteNewLine();
        }
    }

    private void DumpInputReadRequest()
    {
        try
        {
            lock (_lock)
            {
                Write(s_inputReadRequestHeader);

                WriteNewLine();
            }
        }
        catch
        {
            // ignored
        }
    }

    private void DumpExceptionResultObject(ExceptionResultObject result)
    {
        lock (_lock)
        {
            Write(s_exceptionResultHeader);

            using (var jsonWriter = CreateJsonWriter())
            {
                using var _ = jsonWriter.WriteObject();
                jsonWriter.WriteProperty("m", result.Message);
                jsonWriter.WriteProperty("l", result.LineNumber);
                WriteResultObjectContent(jsonWriter, result);
            }

            WriteNewLine();
        }
    }

    private void DumpResultObject(ResultObject result)
    {
        lock (_lock)
        {
            Write(s_resultObjectHeader);

            using (var jsonWriter = CreateJsonWriter())
            {
                WriteResultObject(jsonWriter, result, isRoot: true);
            }

            WriteNewLine();
        }
    }

    private void WriteNewLine() => Write(s_newLine);

    private void WriteResultObject(XmlDictionaryWriter jsonWriter, ResultObject result, bool isRoot)
    {
        using var _ = jsonWriter.WriteObject(name: isRoot ? "root" : "item");
        WriteResultObjectContent(jsonWriter, result);
    }

    private void WriteResultObjectContent(XmlDictionaryWriter jsonWriter, ResultObject result)
    {
        jsonWriter.WriteProperty("t", result.Type);
        jsonWriter.WriteProperty("h", result.Header);
        jsonWriter.WriteProperty("v", result.Value);
        jsonWriter.WriteProperty("x", result.IsExpanded);

        if (result.Children != null)
        {
            using var _ = jsonWriter.WriteArray("c");

            foreach (var child in result.Children)
            {
                WriteResultObject(jsonWriter, child, isRoot: false);
            }
        }
    }

    private void Write(byte[] bytes) => _stream.Write(bytes, 0, bytes.Length);

    /// <summary>
    /// Redirects the console to the Dump method.
    /// </summary>
    private class ConsoleRedirectWriter : TextWriter
    {
        private readonly JsonConsoleDumper _dumper;
        private readonly string? _header;

        public override Encoding Encoding => Encoding.UTF8;

        public ConsoleRedirectWriter(JsonConsoleDumper dumper, string? header = null)
        {
            _dumper = dumper;
            _header = header;
        }

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

        private void Dump(object? value) => _dumper.Dump(new DumpData(value, _header, DumpQuotas.Default));
    }

    private class ConsoleReader : TextReader
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
}
