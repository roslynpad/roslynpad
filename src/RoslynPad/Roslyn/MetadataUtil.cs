using System.Reflection;

namespace RoslynPad.Roslyn;

internal class MetadataUtil
{
    public static string GetAssemblyPath(Assembly assembly) => Path.Combine(AppContext.BaseDirectory, assembly.GetName().Name + ".dll");
}
