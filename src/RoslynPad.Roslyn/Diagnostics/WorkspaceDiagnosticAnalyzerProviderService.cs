using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynPad.Roslyn.Diagnostics
{
    [Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
    internal sealed class WorkspaceDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
    {
        public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
        {
            return new[]
            {
                new HostDiagnosticAnalyzerPackage(LanguageNames.CSharp,
                    ImmutableArray.Create(
                        // Microsoft.CodeAnalysis
                        typeof(Compilation).Assembly.Location,
                        // Microsoft.CodeAnalysis.CSharp
                        typeof(CSharpResources).Assembly.Location,
                        // Microsoft.CodeAnalysis.Features
                        typeof(FeaturesResources).Assembly.Location,
                        // Microsoft.CodeAnalysis.CSharp.Features
                        typeof(CSharpFeaturesResources).Assembly.Location))  
            };
        }

        public IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader()
        {
            return SimpleAnalyzerAssemblyLoader.Instance;
        }
    }

    [ExportWorkspaceService(typeof(IAnalyzerService), ServiceLayer.Host), Shared]
    internal sealed class AnalyzerAssemblyLoaderService : IAnalyzerService
    {
        public IAnalyzerAssemblyLoader GetLoader()
        {
            return SimpleAnalyzerAssemblyLoader.Instance;
        }
    }

    internal class SimpleAnalyzerAssemblyLoader : Microsoft.CodeAnalysis.AnalyzerAssemblyLoader
    {
        public static IAnalyzerAssemblyLoader Instance { get; } = new SimpleAnalyzerAssemblyLoader();

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            return Assembly.Load(AssemblyName.GetAssemblyName(fullPath));
        }
    }
}