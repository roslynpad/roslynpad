using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Morgania.CodeAnalysis.Editor.Diagnostics;

[Export(typeof(IAnalyzerAssemblyLoader)), Shared]
internal class AnalyzerAssemblyLoaderWrapper : IAnalyzerAssemblyLoader, IDisposable
{
    private readonly AnalyzerAssemblyLoader _inner = new();
    
    public void Dispose() => _inner.Dispose();

    public void AddDependencyLocation(string fullPath) => _inner.AddDependencyLocation(fullPath);
    public Assembly LoadFromPath(string fullPath) => _inner.LoadFromPath(fullPath);
}
