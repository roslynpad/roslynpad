using System.Runtime.InteropServices;
using NuGet.Versioning;

namespace RoslynPad.Build;

public class ExecutionPlatform
{
    internal string Name { get; }
    internal string TargetFrameworkMoniker { get; }
    internal NuGetVersion? FrameworkVersion { get; }
    internal Architecture Architecture { get; }
    internal bool IsDotNet { get; }
    internal string Description { get; }

    internal bool IsDotNetFramework => !IsDotNet;

    internal ExecutionPlatform(string name, string targetFrameworkMoniker, NuGetVersion? frameworkVersion, Architecture architecture, bool isDotNet)
    {
        Name = name;
        TargetFrameworkMoniker = targetFrameworkMoniker;
        FrameworkVersion = frameworkVersion;
        Architecture = architecture;
        IsDotNet = isDotNet;
        Description = $"{Name} {FrameworkVersion}";
    }

    public override string ToString() => Description;
}
