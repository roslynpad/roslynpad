using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;

namespace RoslynPad.Roslyn.Diagnostics
{
    [ExportWorkspaceService(typeof(IAnalyzerService), ServiceLayer.Host), Shared]
    internal sealed class AnalyzerAssemblyLoaderService : IAnalyzerService
    {
        public IAnalyzerAssemblyLoader GetLoader()
        {
            return SimpleAnalyzerAssemblyLoader.Instance;
        }
    }
}
