using RoslynPad.Hosting;

namespace RoslynPad.Host
{
    internal static class HostProgram
    {
        private static void Main(string[] args)
        {
            if (args.Length != 3) return;
            ExecutionHost.RunServer(args[0], args[1], int.Parse(args[2]));
        }
    }
}
