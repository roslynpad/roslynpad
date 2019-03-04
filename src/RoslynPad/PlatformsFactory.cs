using System;
using System.Composition;
using System.Collections.Generic;
using Microsoft.Win32;
using RoslynPad.UI;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RoslynPad
{
    [Export(typeof(IPlatformsFactory))]
    internal class PlatformsFactory : IPlatformsFactory
    {
        public IEnumerable<ExecutionPlatform> GetExecutionPlatforms()
        {
            var basePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            var dotnetExe = Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432"), "dotnet", "dotnet.exe");
            if (File.Exists(dotnetExe))
            {
                yield return new ExecutionPlatform("Core x64", "netcoreapp2.2", Architecture.X64, dotnetExe, string.Empty);
            }

            var targetFrameworkName = GetTargetFrameworkName();
            yield return new ExecutionPlatform("Desktop x86", targetFrameworkName, Architecture.X86, string.Empty, string.Empty, isDesktop: true);
            yield return new ExecutionPlatform("Desktop x64", targetFrameworkName, Architecture.X64, string.Empty, string.Empty, isDesktop: true);
        }

        private static string GetTargetFrameworkName()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                var release = ndpKey?.GetValue("Release") as int?;
                if (release != null)
                {
                    return GetTargetFrameworkName(release.Value);
                }
            }

            return string.Empty;
        }

        private static string GetTargetFrameworkName(int releaseKey)
        {
            if (releaseKey >= 461808)
                return "net472";
            if (releaseKey >= 461308)
                return "net471";
            if (releaseKey >= 460798)
                return "net47";
            if (releaseKey >= 394802)
                return "net462";
            if (releaseKey >= 394254)
                return "net461";
            if (releaseKey >= 393295)
                return "net46";

            throw new ArgumentOutOfRangeException(nameof(releaseKey));
        }
    }
}