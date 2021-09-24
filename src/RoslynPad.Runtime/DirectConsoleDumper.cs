using System;
using System.IO;

namespace RoslynPad.Runtime
{
    internal class DirectConsoleDumper : IConsoleDumper
    {
        private readonly object _lock = new();

        public bool SupportsRedirect => false;

        public TextWriter CreateWriter(string? header = null)
        {
            throw new NotSupportedException();
        }

        public void Dump(in DumpData data)
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

        public TextReader CreateReader()
        {
            throw new NotSupportedException();
        }

        private void DumpResultObject(ResultObject resultObject, int indent = 0)
        {
            lock (_lock)
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
        }

        public void Flush()
        {
        }

        public void DumpProgress(ProgressResultObject result)
            => throw new NotSupportedException($"Dumping progress is not supported with {nameof(DirectConsoleDumper)}");
    }
}
