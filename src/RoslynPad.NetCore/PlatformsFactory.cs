using System.Composition;
using System.Collections.Generic;
using RoslynPad.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RoslynPad
{
    [Export(typeof(IPlatformsFactory))]
    internal class PlatformsFactory : IPlatformsFactory
    {
        public IEnumerable<ExecusionPlatform> GetExecutionPlatforms()
        {
            var platform = IntPtr.Size == 8 ? "x64" : "x86";

            yield return new ExecusionPlatform("Core " + platform, Process.GetCurrentProcess().MainModule.FileName, 
                '\"' + Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "RoslynPad.HostNetCore.dll") + '\"');
        }
    }
}