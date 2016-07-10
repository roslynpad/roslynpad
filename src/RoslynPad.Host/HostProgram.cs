using System.Threading;
using RoslynPad.Hosting;

namespace RoslynPad.Host
{
    internal static class HostProgram
    {
        private static void Main(string[] args)
        {
            ExecutionHost.RunServer(args[0], args[1]);
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
