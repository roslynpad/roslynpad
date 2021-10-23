using System.Runtime.InteropServices;
using NuGet.Versioning;

namespace RoslynPad.Build
{
    public class ExecutionPlatform
    {
        internal string Name { get; }
        internal string TargetFrameworkMoniker { get; }
        internal NuGetVersion? FrameworkVersion { get; }
        internal Architecture Architecture { get; }
        internal bool IsCore { get; }
        internal bool IsFramework => !IsCore;

        internal ExecutionPlatform(string name, string targetFrameworkMoniker, NuGetVersion? frameworkVersion, Architecture architecture, bool isCore)
        {
            Name = name;
            TargetFrameworkMoniker = targetFrameworkMoniker;
            FrameworkVersion = frameworkVersion;
            Architecture = architecture;
            IsCore = isCore;
        }

        public override string ToString() => $"{Name} {FrameworkVersion}";
    }
}
