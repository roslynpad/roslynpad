#if NET6_0_OR_GREATER
using System.Text;
using System.Text.Json;

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

    private Utf8JsonWriter CreateJsonWriter() => new(_stream);

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
            DumpResultObject(ResultObject.Create(data.Object, data.Quotas, data.Header, data.Line));
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
                jsonWriter.WriteStartObject();

                if (result.Progress != null)
                {
                    jsonWriter.WriteNumber("p", result.Progress.Value);
                }

                jsonWriter.WriteEndObject();
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
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("v", message);
                jsonWriter.WriteEndObject();
            }

            WriteNewLine();
        }
    }

    internal void DumpInputReadRequest()
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
                jsonWriter.WriteStartObject();
                try
                {
                    jsonWriter.WriteString("m", result.Message);
                    WriteResultObjectContent(jsonWriter, result);
                }
                finally
                {
                    jsonWriter.WriteEndObject();
                }
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
                WriteResultObject(jsonWriter, result);
            }

            WriteNewLine();
        }
    }

    private void WriteNewLine() => Write(s_newLine);

    private void WriteResultObject(Utf8JsonWriter jsonWriter, ResultObject result)
    {
        jsonWriter.WriteStartObject();
        try
        {
            WriteResultObjectContent(jsonWriter, result);
        }
        finally
        {
            jsonWriter.WriteEndObject();
        }
    }

    private void WriteResultObjectContent(Utf8JsonWriter jsonWriter, ResultObject result)
    {
        jsonWriter.WriteString("t", result.Type);
        jsonWriter.WriteString("h", result.Header);
        if (result.LineNumber is int lineNumber)
        {
            jsonWriter.WriteNumber("l", lineNumber);
        }

        jsonWriter.WriteString("v", result.Value);
        jsonWriter.WriteBoolean("x", result.IsExpanded);

        if (result.Children != null)
        {
            jsonWriter.WriteStartArray("c");

            try
            {
                foreach (var child in result.Children)
                {
                    WriteResultObject(jsonWriter, child);
                }
            }
            finally
            {
                jsonWriter.WriteEndArray();
            }
        }
    }

    private void Write(byte[] bytes) => _stream.Write(bytes, 0, bytes.Length);
}
#endif
