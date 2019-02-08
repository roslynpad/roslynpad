using System;
using System.Composition;
using System.Collections.Generic;
using Microsoft.Win32;
using RoslynPad.UI;
using System.IO;
using System.Diagnostics;

namespace RoslynPad
{
    [Export(typeof(IPlatformsFactory))]
    internal class PlatformsFactory : IPlatformsFactory
    {
        public IEnumerable<ExecutionPlatform> GetExecutionPlatforms()
        {
            var basePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

            var targetFrameworkName = GetTargetFrameworkName();
            yield return new ExecutionPlatform("Desktop x86", targetFrameworkName, Path.Combine(basePath, "RoslynPad.Host32.exe"), string.Empty, useDesktopReferences: true);
            yield return new ExecutionPlatform("Desktop x64", targetFrameworkName, Path.Combine(basePath, "RoslynPad.Host64.exe"), string.Empty, useDesktopReferences: true);

            var netCoreHost = Path.Combine(basePath, "NetCoreHost", "RoslynPad.HostNetCore.exe");
            // requires .NET Core 3 SDK which produces an EXE
            if (File.Exists(netCoreHost))
            {
                yield return new ExecutionPlatform("Core x64", "netcoreapp2.2", netCoreHost, string.Empty);
            }
            else
            {
                var dotnetExe = Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432"), "dotnet", "dotnet.exe");
                if (File.Exists(dotnetExe))
                {
                    yield return new ExecutionPlatform("Core x64", "netcoreapp2.2", dotnetExe, Path.Combine(basePath, "NetCoreHost", "RoslynPad.HostNetCore.dll"));
                }
            }
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