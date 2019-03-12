using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    [Export(typeof(IAnalyzerAssemblyLoader)), Shared]
    public class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        // TODO: .NET Core?
        private readonly IAnalyzerAssemblyLoader _impl = new DesktopAnalyzerAssemblyLoader();

        public void AddDependencyLocation(string fullPath)
        {
            _impl.AddDependencyLocation(fullPath);
        }

        public Assembly LoadFromPath(string fullPath)
        {
            return _impl.LoadFromPath(fullPath);
        }
    }
}
