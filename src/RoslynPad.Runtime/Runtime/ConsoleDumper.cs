using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace RoslynPad.Runtime
{
    internal interface IConsoleDumper
    {
        bool SupportsRedirect { get; }
        TextWriter CreateWriter(string? header = null);
        void Dump(DumpData data);
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

    internal class JsonConsoleDumper : IDisposable, IConsoleDumper
    {
        private const int MaxDumpsPerSession = 100000;

        private static readonly byte[] NewLine = Encoding.Default.GetBytes(Environment.NewLine);

        private readonly DataContractJsonSerializer _serializer;
        private readonly Stream _stream;

        private int _dumpCount;

        public JsonConsoleDumper()
        {
            // this assembly shouldn't have any external dependencies, so using this legacy serializer
            _serializer = new DataContractJsonSerializer(typeof(ResultObject));

            _stream = Console.OpenStandardOutput();
        }

        public bool SupportsRedirect => true;

        public TextWriter CreateWriter(string? header = null)
        {
            return new ConsoleRedirectWriter(this, header);
        }

        public void Dispose()
        {
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
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(_stream, Encoding.UTF8, ownsStream: false))
            {
                writer.WriteStartElement("root", "");
                writer.WriteAttributeString("type", "object");
                writer.WriteElementString(nameof(ResultObject.Value), message);
                writer.WriteEndElement();
            }
        }

        private void DumpResultObject(ResultObject result)
        {
            _serializer.WriteObject(_stream, result);
            _stream.Write(NewLine, 0, NewLine.Length);
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
