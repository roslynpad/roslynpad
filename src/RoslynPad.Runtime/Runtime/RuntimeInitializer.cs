using RoslynPad.Utilities;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RoslynPad.Runtime
{
    /// <summary>
    /// This class initializes the RoslynPad standalone host.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class RuntimeInitializer
    {
        private static bool _initialized;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var isAttachedToParent = TryAttachToParentProcess();
            DisableWer();
            AttachConsole(isAttachedToParent);
        }

        private static void AttachConsole(bool isAttachedToParent)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            var consoleDumper = isAttachedToParent ? (IConsoleDumper)new JsonConsoleDumper() : new DirectConsoleDumper();

            if (consoleDumper.SupportsRedirect)
            {
                Console.SetOut(consoleDumper.CreateWriter());
                Console.SetError(consoleDumper.CreateWriter("Error"));
                Console.SetIn(consoleDumper.CreateReader());

                AppDomain.CurrentDomain.UnhandledException += (o, e) =>
                {
                    consoleDumper.DumpException((Exception)e.ExceptionObject);
                    Environment.Exit(1);
                };

                Helpers.Progress += progress => consoleDumper.DumpProgress(ProgressResultObject.Create(progress));
            }

            ObjectExtensions.Dumped += data => consoleDumper.Dump(data);
            AppDomain.CurrentDomain.ProcessExit += (o, e) => consoleDumper.Flush();            
        }

        private static bool TryAttachToParentProcess()
        {
            if (ParseCommandLine("pid", @"\d+", out var parentProcessId))
            {
                AttachToParentProcess(int.Parse(parentProcessId));

                return true;
            }

            return false;
        }

        internal static void AttachToParentProcess(int parentProcessId)
        {
            Process clientProcess;
            try
            {
                clientProcess = Process.GetProcessById(parentProcessId);
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
                WindowsNativeMethods.DisableWer();
            }
        }

        private static bool ParseCommandLine(string name, string pattern, out string value)
        {
            var match = Regex.Match(Environment.CommandLine, @$"--{name}\s+""?({pattern})");
            value = match.Success ? match.Groups[1].Value : string.Empty;
            return match.Success;
        }
    }
}
