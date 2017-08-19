namespace RoslynPad.UI
{
    public class ExecutionPlatform
    {
        public string Name { get; }
        public string HostPath { get; }
        public string HostArguments { get; }

        public ExecutionPlatform(string name, string hostPath, string hostArguments)
        {
            Name = name;
            HostPath = hostPath;
            HostArguments = hostArguments;
        }

        public override string ToString() => Name;
    }
}