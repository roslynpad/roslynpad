using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Xml;

namespace RoslynPad.Runtime
{
    internal interface IConsoleDumper
    {
        bool SupportsRedirect { get; }
        TextWriter CreateWriter(string? header = null);
        void Dump(DumpData data);
        void DumpException(Exception exception);
        void Flush();
    }

    internal class DirectConsoleDumper : IConsoleDumper
    {
        public bool SupportsRedirect => false;

        public TextWriter CreateWriter(string? header = null)
        {
            throw new NotSupportedException();
        }

        public void Dump(DumpData data)
        {
            try
            {
                DumpResultObject(ResultObject.Create(data.Object, data.Quotas, data.Header));
            }
            catch (Exception ex)
            {
                try
                {
                    Console.WriteLine("Error during Dump: " + ex.Message);
                }
                catch
                {
                    // ignore
                }
            }
        }

        public void DumpException(Exception exception)
        {
            throw new NotSupportedException();
        }

        private void DumpResultObject(ResultObject resultObject, int indent = 0)
        {
            if (indent > 0)
            {
                Console.Write("".PadLeft(indent));
            }

            Console.Write(resultObject.HasChildren ? "+ " : "  ");

            if (resultObject.Header != null)
            {
                Console.Write($"[{resultObject.Header}]: ");
            }

            Console.WriteLine(resultObject.Value);

            if (resultObject.Children != null)
            {
                foreach (var child in resultObject.Children)
                {
                    DumpResultObject(child, indent + 2);
                }
            }

            if (indent == 0)
            {
                Console.WriteLine();
            }
        }

        public void Flush()
        {
        }
    }

    internal class JsonConsoleDumper : IConsoleDumper, IDisposable
    {
        private const int MaxDumpsPerSession = 100000;

        private static readonly byte[] NewLine = Encoding.Default.GetBytes(Environment.NewLine);

        private readonly string _exceptionResultTypeName;
        private readonly Stream _stream;
        private readonly XmlDictionaryWriter _jsonWriter;

        private int _dumpCount;

        public JsonConsoleDumper()
        {
            _stream = Console.OpenStandardOutput();

            // this assembly shouldn't have any external dependencies, so using this legacy JSON writer
            _jsonWriter = JsonReaderWriterFactory.CreateJsonWriter(_stream, Encoding.UTF8, ownsStream: false);

            _exceptionResultTypeName = $"{typeof(ExceptionResultObject).FullName}, {typeof(ExceptionResultObject).Assembly.GetName().Name}";
        }

        public bool SupportsRedirect => true;

        public TextWriter CreateWriter(string? header = null)
        {
            return new ConsoleRedirectWriter(this, header);
        }

        public void Dispose()
        {
            _jsonWriter.Dispose();
            _stream.Dispose();
        }

        public void Dump(DumpData data)
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

        public void Flush()
        {
            _stream.Flush();
        }

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

        protected void DumpMessage(string message)
        {
            _jsonWriter.WriteStartElement("root", "");
            _jsonWriter.WriteAttributeString("type", "object");
            _jsonWriter.WriteElementString("v", message);
            _jsonWriter.WriteEndElement();
            _jsonWriter.Flush();

            DumpNewLine();
        }

        private void DumpExceptionResultObject(ExceptionResultObject result)
        {
            _jsonWriter.WriteStartElement("root", "");
            _jsonWriter.WriteAttributeString("type", "object");
            _jsonWriter.WriteElementString("$type", _exceptionResultTypeName);
            _jsonWriter.WriteElementString("m", result.Message);
            _jsonWriter.WriteStartElement("l");
            _jsonWriter.WriteValue(result.LineNumber);
            _jsonWriter.WriteEndElement();
            WriteResultObjectContent(result);
            _jsonWriter.WriteEndElement();
            _jsonWriter.Flush();

            DumpNewLine();
        }

        private void DumpResultObject(ResultObject result)
        {
            WriteResultObject(result, isRoot: true);
            _jsonWriter.Flush();

            DumpNewLine();
        }

        private void DumpNewLine()
        {
            _stream.Write(NewLine, 0, NewLine.Length);
        }

        private void WriteResultObject(ResultObject result, bool isRoot)
        {
            _jsonWriter.WriteStartElement(isRoot ? "root" : "item", "");
            _jsonWriter.WriteAttributeString("type", "object");
            WriteResultObjectContent(result);
            _jsonWriter.WriteEndElement();
        }

        private void WriteResultObjectContent(ResultObject result)
        {
            _jsonWriter.WriteElementString("t", result.Type);
            _jsonWriter.WriteElementString("h", result.Header);
            _jsonWriter.WriteElementString("v", result.Value);
            _jsonWriter.WriteStartElement("x");
            _jsonWriter.WriteValue(result.IsExpanded);
            _jsonWriter.WriteEndElement();

            if (result.Children != null)
            {
                _jsonWriter.WriteStartElement("c");
                _jsonWriter.WriteAttributeString("type", "array");

                foreach (var child in result.Children)
                {
                    WriteResultObject(child, isRoot: false);
                }

                _jsonWriter.WriteEndElement();
            }
        }

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

            public override void Write(string value)
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
                    if (count >= Environment.NewLine.Length &&
                        EndsWithNewLine(buffer, index, count))
                    {
                        count -= Environment.NewLine.Length;
                    }

                    Dump(new string(buffer, index, count));
                }
            }

            private bool EndsWithNewLine(char[] buffer, int index, int count)
            {
                var nl = Environment.NewLine;

                for (int i = nl.Length; i >= 1; --i)
                {
                    if (buffer[index + count - i] != nl[nl.Length - i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override void Write(char value)
            {
                Dump(value);
            }

            private void Dump(object value)
            {
                _dumper.Dump(new DumpData(value, _header, DumpQuotas.Default));
            }
        }
    }
}
