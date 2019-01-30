using System.Composition;
using System.Collections.Generic;
using RoslynPad.UI;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RoslynPad
{
    [Export(typeof(IPlatformsFactory))]
    internal class PlatformsFactory : IPlatformsFactory
    {
        public IEnumerable<ExecutionPlatform> GetExecutionPlatforms()
        {
            var platform = IntPtr.Size == 8 ? "x64" : "x86";

            var exeNoExt = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "RoslynPad.HostNetCore");
            var exe = exeNoExt;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                exe += ".exe";
            }

            if (File.Exists(exe))
            {
                yield return new ExecutionPlatform("Core " + platform, "netcoreapp2.1", exe, string.Empty);
            }
            else
            {
                var processExe = Process.GetCurrentProcess().MainModule.FileName;
                if (Path.GetFileNameWithoutExtension(processExe).Equals("dotnet", StringComparison.InvariantCultureIgnoreCase))
                {
                    yield return new ExecutionPlatform("Core " + platform, "netcoreapp2.1",
                        processExe,
                        exeNoExt + ".dll");
                }
            }
        }
    }
}