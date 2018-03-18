namespace RoslynPad.UI
{
    public class ExecutionPlatform
    {
        public string Name { get; }
        public string TargetFrameworkName { get; set; }
        public string HostPath { get; }
        public string HostArguments { get; }

        public ExecutionPlatform(string name, string targetFrameworkName, string hostPath, string hostArguments)
        {
            Name = name;
            TargetFrameworkName = targetFrameworkName;
            HostPath = hostPath;
            HostArguments = hostArguments;
        }

        public override string ToString() => Name;
    }
}