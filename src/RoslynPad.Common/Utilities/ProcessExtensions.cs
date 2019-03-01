using System.Diagnostics;

namespace RoslynPad.Utilities
{
    internal static class ProcessExtensions
    {
        public static bool IsAlive(this Process process)
        {
            try
            {
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }
}