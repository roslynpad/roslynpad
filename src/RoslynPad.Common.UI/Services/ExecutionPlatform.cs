namespace RoslynPad.UI
{
    public class ExecutionPlatform
    {
        public string Name { get; }
        public string TargetFrameworkName { get; }
        public string HostPath { get; }
        public string HostArguments { get; }
        public bool UseDesktopReferences { get; }

        public ExecutionPlatform(string name, string targetFrameworkName, string hostPath, string hostArguments, bool useDesktopReferences = false)
        {
            Name = name;
            TargetFrameworkName = targetFrameworkName;
            HostPath = hostPath;
            HostArguments = hostArguments;
            UseDesktopReferences = useDesktopReferences;
        }

        public override string ToString() => Name;
    }
}