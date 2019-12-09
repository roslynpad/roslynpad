using Microsoft.Win32;
using NuGet.Versioning;
using RoslynPad.UI;
using RoslynPad.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RoslynPad
{
    [Export(typeof(IPlatformsFactory))]
    internal class PlatformsFactory : IPlatformsFactory
    {
        public IEnumerable<ExecutionPlatform> GetExecutionPlatforms()
        {
            if (GetCoreVersions() is var core && core.versions.Count > 0)
            {
                yield return new ExecutionPlatform(".NET Core x64", "", core.versions, Architecture.X64, core.dotnetExe, string.Empty);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetFrameworkName = GetNetFrameworkName();
                yield return new ExecutionPlatform(".NET Framework x86", targetFrameworkName, Array.Empty<PlatformVersion>(), Architecture.X86, string.Empty, string.Empty, isDesktop: true);
                yield return new ExecutionPlatform(".NET Framework x64", targetFrameworkName, Array.Empty<PlatformVersion>(), Architecture.X64, string.Empty, string.Empty, isDesktop: true);
            }
        }

        private (IReadOnlyList<PlatformVersion> versions, string dotnetExe) GetCoreVersions()
        {
            string[] dotnetPaths;
            string dotnetExe;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dotnetPaths = new[] { Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet") };
                dotnetExe = "dotnet.exe";
            }
            else
            {
                dotnetPaths = new[] { "/usr/share/dotnet", "/usr/local/share/dotnet" };
                dotnetExe = "dotnet";
            }

            var sdkPath = (from path in dotnetPaths
                          let fullPath = Path.Combine(path, "sdk")
                          where Directory.Exists(fullPath)
                          select fullPath).FirstOrDefault();

            dotnetExe = Path.GetFullPath(Path.Combine(sdkPath, "..", dotnetExe));

            if (sdkPath != null && File.Exists(dotnetExe))
            {
                var sortedDictionary = new SortedDictionary<NuGetVersion, PlatformVersion>();

                foreach (var directory in IOUtilities.EnumerateDirectories(sdkPath))
                {
                    var versionName = Path.GetFileName(directory);
                    if (NuGetVersion.TryParse(versionName, out var version) && version.Major > 1)
                    {
                        sortedDictionary.Add(version, new PlatformVersion($"netcoreapp{version.Major}.{version.Minor}", versionName));
                    }
                }

                return (sortedDictionary.Values.Reverse().ToImmutableArray(),
                        dotnetExe);
            }

            return (ImmutableArray<PlatformVersion>.Empty, string.Empty);
        }

        private static string GetNetFrameworkName()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                var release = ndpKey?.GetValue("Release") as int?;
                if (release != null)
                {
                    return GetNetFrameworkTargetName(release.Value);
                }
            }

            return string.Empty;
        }

        private static string GetNetFrameworkTargetName(int releaseKey)
        {
            if (releaseKey >= 528040)
                return "net48";
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