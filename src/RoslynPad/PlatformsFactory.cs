using System;
using System.Composition;
using System.Collections.Generic;
using Microsoft.Win32;
using RoslynPad.UI;
using System.IO;
using System.Runtime.InteropServices;
using RoslynPad.Utilities;
using NuGet.Versioning;
using System.Collections.Immutable;
using System.Linq;

namespace RoslynPad
{
    [Export(typeof(IPlatformsFactory))]
    internal class PlatformsFactory : IPlatformsFactory
    {
        public IEnumerable<ExecutionPlatform> GetExecutionPlatforms()
        {
            var dotnetPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet");
            var dotnetExe = Path.Combine(dotnetPath, "dotnet.exe");
            if (File.Exists(dotnetExe) &&
                GetCoreVersions(dotnetPath) is var coreVersions && coreVersions.Count > 0)
            {
                yield return new ExecutionPlatform("Core x64", "", coreVersions, Architecture.X64, dotnetExe, string.Empty);
            }

            var targetFrameworkName = GetTargetFrameworkName();
            yield return new ExecutionPlatform("Desktop x86", targetFrameworkName, Array.Empty<PlatformVersion>(), Architecture.X86, string.Empty, string.Empty, isDesktop: true);
            yield return new ExecutionPlatform("Desktop x64", targetFrameworkName, Array.Empty<PlatformVersion>(), Architecture.X64, string.Empty, string.Empty, isDesktop: true);
        }

        private IReadOnlyList<PlatformVersion> GetCoreVersions(string dotnetPath)
        {
            const string frameworkName = "Microsoft.NETCore.App";

            var path = Path.Combine(dotnetPath, "shared", frameworkName);

            var sortedDictionary = new SortedDictionary<NuGetVersion, PlatformVersion>();

            foreach (var directory in IOUtilities.EnumerateDirectories(path))
            {
                var versionName = Path.GetFileName(directory);
                if (NuGetVersion.TryParse(versionName, out var version) && version.Major > 1)
                {
                    sortedDictionary.Add(version, new PlatformVersion($"netcoreapp{version.Major}.{version.Minor}", frameworkName, versionName));
                }
            }

            return sortedDictionary.Values.Reverse().ToImmutableArray();
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