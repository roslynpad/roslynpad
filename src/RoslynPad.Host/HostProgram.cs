using RoslynPad.Hosting;

namespace RoslynPad.Host
{
    internal static class HostProgram
    {
        private static void Main(string[] args)
        {
            if (args.Length != 4) return;
            ExecutionHost.RunServer(args[0], args[1], args[2], int.Parse(args[3]));
        }
    }
}
