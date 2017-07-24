using System.Reflection;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    internal sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public Assembly LoadFromPath(string fullPath)
        {
            return AssemblyUtilities.Load(fullPath);
        }

        public void AddDependencyLocation(string fullPath)
        {
        }
    }
}