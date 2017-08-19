using System.Reflection;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn
{
    // TODO: check on which platforms these can fail
    public static class AssemblyUtilities
    {
        public static string GetLocation(this Assembly assembly)
        {
            return CorLightup.Desktop.GetAssemblyLocation(assembly);
        }
    }
}