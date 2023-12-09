using Microsoft.Win32;
using NuGet.Versioning;
using RoslynPad.Build;
using RoslynPad.UI;
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RoslynPad;

[Export(typeof(IPlatformsFactory))]
internal class PlatformsFactory : IPlatformsFactory
{
    IReadOnlyList<ExecutionPlatform>? _executionPlatforms;
    private string? _dotnetExe;
    private string? _sdkPath;

    public IReadOnlyList<ExecutionPlatform> GetExecutionPlatforms() =>
        _executionPlatforms ??= GetNetVersions().Concat(GetNetFrameworkVersions()).ToArray().AsReadOnly();

    public string DotNetExecutable => FindNetSdk().dotnetExe;

    private IEnumerable<ExecutionPlatform> GetNetVersions()
    {
        var (_, sdkPath) = FindNetSdk();

        if (string.IsNullOrEmpty(sdkPath))
        {
            return Array.Empty<ExecutionPlatform>();
        }

        var versions = new List<(string name, string tfm, NuGetVersion version)>();

        foreach (var directory in IOUtilities.EnumerateDirectories(sdkPath))
        {
            var versionName = Path.GetFileName(directory);
            if (NuGetVersion.TryParse(versionName, out var version) && version.Major > 1)
            {
                var name = version.Major < 5 ? ".NET Core" : ".NET";
                var tfm = version.Major < 5 ? $"netcoreapp{version.Major}.{version.Minor}" : $"net{version.Major}.{version.Minor}";
                versions.Add((name, tfm, version));
            }
        }

        return versions.OrderBy(c => c.version.IsPrerelease).ThenByDescending(c => c.version)
            .Select(version => new ExecutionPlatform(version.name, version.tfm, version.version, Architecture.X64, isDotNet: true));
    }

    private IEnumerable<ExecutionPlatform> GetNetFrameworkVersions()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RuntimeInformation.OSArchitecture == Architecture.X64)
        {
            var targetFrameworkName = GetNetFrameworkName();
            yield return new ExecutionPlatform(".NET Framework x64", targetFrameworkName, null, Architecture.X64, isDotNet: false);
        }
    }

    private (string dotnetExe, string sdkPath) FindNetSdk()
    {
        if (_dotnetExe != null && _sdkPath != null)
        {
            return (_dotnetExe, _sdkPath);
        }

        string[] dotnetPaths;
        string dotnetExe;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            dotnetPaths = [Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet")];
            dotnetExe = "dotnet.exe";
        }
        else
        {
            dotnetPaths = ["/usr/lib64/dotnet", "/usr/share/dotnet", "/usr/local/share/dotnet", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet")];
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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return string.Empty;
        }

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

    private static string GetNetFrameworkTargetName(int releaseKey) => releaseKey switch
    {
        >= 528040 => "net48",
        >= 461808 => "net472",
        >= 461308 => "net471",
        >= 460798 => "net47",
        >= 394802 => "net462",
        >= 394254 => "net461",
        >= 393295 => "net46",
        _ => throw new ArgumentOutOfRangeException(nameof(releaseKey))
    };
}
