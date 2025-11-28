using Microsoft.Win32;
using NuGet.Versioning;
using RoslynPad.Build;
using RoslynPad.UI;
using System.Composition;
using System.Runtime.InteropServices;

namespace RoslynPad;

[Export(typeof(IPlatformsFactory))]
internal class PlatformsFactory : IPlatformsFactory
{
    IReadOnlyList<ExecutionPlatform>? _executionPlatforms;
    private (string dotnetExe, string sdkPath) _dotnetPaths;

    public IReadOnlyList<ExecutionPlatform> GetExecutionPlatforms() =>
        _executionPlatforms ??= GetNetVersions().Concat(GetNetFrameworkVersions()).ToArray().AsReadOnly();

    public string DotNetExecutable => FindNetSdk().dotnetExe;

    private IEnumerable<ExecutionPlatform> GetNetVersions()
    {
        var (_, sdkPath) = FindNetSdk();

        if (string.IsNullOrEmpty(sdkPath))
        {
            return [];
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
        if (_dotnetPaths.dotnetExe is not null)
        {
            return _dotnetPaths;
        }

        List<string> dotnetPaths = [];
        if (Environment.GetEnvironmentVariable("DOTNET_ROOT") is var dotnetRoot && !string.IsNullOrEmpty(dotnetRoot))
        {
            dotnetPaths.Add(dotnetRoot);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            dotnetPaths.Add(Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet"));
        }
        else
        {
            dotnetPaths.AddRange([
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet"),
                "/usr/lib/dotnet",
                "/usr/lib64/dotnet",
                "/usr/share/dotnet",
                "/usr/local/share/dotnet",
            ]);
        }

        var dotnetExe = GetDotnetExe();
        var paths = (from path in dotnetPaths
                     let exePath = Path.Combine(path, dotnetExe)
                     let fullPath = Path.Combine(path, "sdk")
                     where File.Exists(exePath) && Directory.Exists(fullPath)
                     select (exePath, fullPath)).FirstOrDefault<(string exePath, string fullPath)>();

        if (paths.exePath is null)
        {
            paths = (string.Empty, string.Empty);
        }

        _dotnetPaths = paths;
        return paths;
    }

    private static string GetDotnetExe() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

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
