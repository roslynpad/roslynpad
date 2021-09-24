using System;
using System.Runtime.InteropServices;

namespace RoslynPad.Build
{
    public class ExecutionPlatform
    {
        internal string Name { get; }
        internal string TargetFrameworkMoniker { get; }
        internal Version? FrameworkVersion { get; }
        internal Architecture Architecture { get; }
        internal bool IsCore { get; }
        internal bool IsFramework => !IsCore;

        internal ExecutionPlatform(string name, string targetFrameworkMoniker, Version? frameworkVersion, Architecture architecture, bool isCore)
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
