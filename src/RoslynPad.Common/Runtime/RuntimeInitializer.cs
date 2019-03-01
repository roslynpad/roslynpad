using RoslynPad.Utilities;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace RoslynPad.Runtime
{
    /// <summary>
    /// This class initializes the RoslynPad standalone host.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class RuntimeInitializer
    {
        private const int MaxDumpsPerSession = 100000;

        private static readonly byte[] NewLine = Encoding.Default.GetBytes(Environment.NewLine);

        private static bool _initialized;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            AttachToClientProcess(int.Parse(Environment.GetCommandLineArgs()[1]));
            DisableWer();

            Console.SetOut(new ConsoleRedirectWriter());
            Console.SetError(new ConsoleRedirectWriter("Error"));

            // this assembly shouldn't have any external dependencies, so using this legacy serializer
            var serializer = new DataContractJsonSerializer(typeof(ResultObject));

            var dumpCount = 0;

            ObjectExtensions.Dumped += data =>
            {
                var currentCount = Interlocked.Increment(ref dumpCount);
                if (currentCount >= MaxDumpsPerSession)
                {
                    if (currentCount == MaxDumpsPerSession)
                    {
                        Dump(new DumpData("<max results reached>", null, DumpQuotas.Default), serializer);
                    }

                    return;
                }

                try
                {
                    Dump(data, serializer);
                }
                catch (Exception ex)
                {
                    try
                    {
                        Dump(new DumpData(ex.Message, "Dump Error", DumpQuotas.Default), serializer);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
                ExceptionResultObject.Create((Exception)e.ExceptionObject).Dump();
        }

        private static void Dump(DumpData data, DataContractJsonSerializer serializer)
        {
            var result = data.Object as ResultObject ?? ResultObject.Create(data.Object, data.Quotas, data.Header);

            using (var stream = Console.OpenStandardOutput())
            {
                serializer.WriteObject(stream, result);
                stream.Write(NewLine, 0, NewLine.Length);
            }
        }

        internal static void AttachToClientProcess(int clientProcessId)
        {
            Process clientProcess;
            try
            {
                clientProcess = Process.GetProcessById(clientProcessId);
            }
            catch (ArgumentException)
            {
                Environment.Exit(1);
                return;
            }

            clientProcess.EnableRaisingEvents = true;
            clientProcess.Exited += (o, e) =>
            {
                Environment.Exit(1);
            };

            if (!clientProcess.IsAlive())
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Disables Windows Error Reporting for the process, so that the process fails fast.
        /// </summary>
        internal static void DisableWer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    SetErrorMode(GetErrorMode() | ErrorMode.SEM_FAILCRITICALERRORS | ErrorMode.SEM_NOOPENFILEERRORBOX |
                                 ErrorMode.SEM_NOGPFAULTERRORBOX);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private class ConsoleRedirectWriter : TextWriter
        {
            private readonly string? _header;

            public override Encoding Encoding => Encoding.UTF8;

            public ConsoleRedirectWriter(string? header = null)
            {
                _header = header;
            }

            public override void Write(string value)
            {
                if (string.Equals(Environment.NewLine, value, StringComparison.Ordinal))
                {
                    return;
                }

                value.Dump(_header);
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

                    new string(buffer, index, count).Dump(_header);
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
                value.Dump(_header);
            }
        }

        #region Win32 API

        [DllImport("kernel32", PreserveSig = true)]
        internal static extern ErrorMode SetErrorMode(ErrorMode mode);

        [DllImport("kernel32", PreserveSig = true)]
        internal static extern ErrorMode GetErrorMode();

        [Flags]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal enum ErrorMode
        {
            SEM_FAILCRITICALERRORS = 0x0001,

            SEM_NOGPFAULTERRORBOX = 0x0002,

            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,

            SEM_NOOPENFILEERRORBOX = 0x8000,
        }

        #endregion
    }
}
