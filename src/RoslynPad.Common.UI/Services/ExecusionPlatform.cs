namespace RoslynPad.UI
{
    public class ExecusionPlatform
    {
        public string Name { get; }
        public string HostPath { get; }
        public string HostArguments { get; }

        public ExecusionPlatform(string name, string hostPath, string hostArguments)
        {
            Name = name;
            HostPath = hostPath;
            HostArguments = hostArguments;
        }

        public override string ToString() => Name;
    }
}