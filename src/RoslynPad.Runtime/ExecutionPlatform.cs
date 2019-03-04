using System.Runtime.InteropServices;

namespace RoslynPad
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class ExecutionPlatform
    {
        internal string Name { get; }
        internal string TargetFrameworkName { get; }
        internal Architecture Architecture { get; }
        internal string HostPath { get; }
        internal string HostArguments { get; }
        internal bool IsDesktop { get; }
        internal bool IsCore => !IsDesktop;

        internal ExecutionPlatform(string name, string targetFrameworkName, Architecture architecture, string hostPath, string hostArguments, bool isDesktop = false)
        {
            Name = name;
            TargetFrameworkName = targetFrameworkName;
            Architecture = architecture;
            HostPath = hostPath;
            HostArguments = hostArguments;
            IsDesktop = isDesktop;
        }

        public override string ToString() => Name;
    }
}