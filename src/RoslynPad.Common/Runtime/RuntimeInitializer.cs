using RoslynPad.Utilities;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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

            AttachToParentProcess();
            DisableWer();
            ApplyDepsFile();
            AttachConsole();
        }

        private static void AttachConsole()
        {
            var consoleDumper = new ConsoleDumper();
            Console.SetOut(consoleDumper.CreateWriter());
            Console.SetError(consoleDumper.CreateWriter("Error"));
            ObjectExtensions.Dumped += data => consoleDumper.Dump(data);
            AppDomain.CurrentDomain.ProcessExit += (o, e) => consoleDumper.Flush();
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
                ExceptionResultObject.Create((Exception)e.ExceptionObject).Dump();
        }

        private static void AttachToParentProcess()
        {
            if (ParseCommandLine("pid", @"\d+", out var parentProcessId))
            {
                AttachToParentProcess(int.Parse(parentProcessId));
            }
        }

        private static void ApplyDepsFile()
        {
            if (RuntimeInformation.FrameworkDescription.IndexOf(".NET Framework", StringComparison.Ordinal) >= 0 &&
                ParseCommandLine("depsfile", @"[^\s""]+", out var depsFile))
            {
                var depsLoader = new DepsLoader(depsFile);
                AppDomain.CurrentDomain.AssemblyResolve += (o, e) => depsLoader.TryLoadAssembly(e.Name);
            }
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
