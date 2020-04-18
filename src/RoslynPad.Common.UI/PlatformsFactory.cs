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
        private string? _dotnetExe;
        private string? _sdkPath;

        public event Action Changed = delegate { };

        public IEnumerable<ExecutionPlatform> GetExecutionPlatforms()
        {
            if (GetCoreVersions() is var core)
            {
                foreach (var version in core.versions)
                {
                    yield return new ExecutionPlatform(".NET Core", version.tfm, version.name, Architecture.X64, isCore: true);
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var targetFrameworkName = GetNetFrameworkName();
                yield return new ExecutionPlatform(".NET Framework x86", targetFrameworkName, string.Empty, Architecture.X86, isCore: false);
                yield return new ExecutionPlatform(".NET Framework x64", targetFrameworkName, string.Empty, Architecture.X64, isCore: false);
            }
        }

        public string DotNetExecutable => FindNetCore().dotnetExe;

        private (IReadOnlyList<(string tfm, string name)> versions, string dotnetExe) GetCoreVersions()
        {
            var (dotnetExe, sdkPath) = FindNetCore();

            if (!string.IsNullOrEmpty(dotnetExe))
            {
                var dictionary = new Dictionary<NuGetVersion, (string tfm, string name)>();

                foreach (var directory in IOUtilities.EnumerateDirectories(sdkPath))
                {
                    var versionName = Path.GetFileName(directory);
                    if (NuGetVersion.TryParse(versionName, out var version) && version.Major > 1)
                    {
                        dictionary.Add(version, ($"netcoreapp{version.Major}.{version.Minor}", versionName));
                    }
                }

                return (dictionary.OrderBy(c => c.Key.IsPrerelease).ThenByDescending(c => c.Key).Select(c => c.Value).ToImmutableArray(),
                        dotnetExe);
            }

            return (ImmutableArray<(string, string)>.Empty, string.Empty);
        }

        private (string dotnetExe, string sdkPath) FindNetCore()
        {
            if (_dotnetExe != null && _sdkPath != null)
            {
                return (_dotnetExe, _sdkPath);
            }

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

            if (sdkPath != null)
            {
                dotnetExe = Path.GetFullPath(Path.Combine(sdkPath, "..", dotnetExe));
                if (File.Exists(dotnetExe))
                {
                    _dotnetExe = dotnetExe;
                    _sdkPath = sdkPath;
                    return (dotnetExe, sdkPath);
                }
            }

            _dotnetExe = string.Empty;
            _sdkPath = string.Empty;

            return (string.Empty, string.Empty);
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