using System.Runtime.InteropServices;

namespace RoslynPad
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class ExecutionPlatform
    {
        internal string Name { get; }
        internal string TargetFrameworkMoniker { get; }
        internal string FrameworkVersion { get; }
        internal Architecture Architecture { get; }
        internal bool IsCore { get; }
        internal bool IsFramework => !IsCore;

        internal ExecutionPlatform(string name, string targetFrameworkMoniker, string frameworkVersion, Architecture architecture, bool isCore)
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