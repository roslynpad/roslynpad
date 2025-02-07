using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Diagnostics;

[Export(typeof(IAnalyzerAssemblyLoader)), Shared]
internal class AnalyzerAssemblyLoaderWrapper : IAnalyzerAssemblyLoader, IDisposable
{
    private readonly DefaultAnalyzerAssemblyLoader _inner = new();
    
    public void Dispose() => _inner.Dispose();

    public void AddDependencyLocation(string fullPath) => _inner.AddDependencyLocation(fullPath);
    public Assembly LoadFromPath(string fullPath) => _inner.LoadFromPath(fullPath);
}
