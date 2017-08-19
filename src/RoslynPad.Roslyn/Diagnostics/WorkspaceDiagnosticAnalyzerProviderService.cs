using System;
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
                        typeof(Compilation).GetTypeInfo().Assembly.GetLocation(),
                        // Microsoft.CodeAnalysis.CSharp
                        typeof(CSharpCompilation).GetTypeInfo().Assembly.GetLocation(),
                        // Microsoft.CodeAnalysis.Features
                        typeof(FeaturesResources).GetTypeInfo().Assembly.GetLocation(),
                        // Microsoft.CodeAnalysis.CSharp.Features
                        typeof(CSharpFeaturesResources).GetTypeInfo().Assembly.GetLocation()))  
            };
        }

        public IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader()
        {
            return SimpleAnalyzerAssemblyLoader.Instance;
        }
    }

    [ExportWorkspaceService(typeof(IAnalyzerService), ServiceLayer.Host), Shared]
    internal sealed class AnalyzerAssemblyLoaderService : IAnalyzerService, IWorkspaceService
    {
        public IAnalyzerAssemblyLoader GetLoader()
        {
            return SimpleAnalyzerAssemblyLoader.Instance;
        }
    }

    internal class SimpleAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        public static readonly IAnalyzerAssemblyLoader Instance = new SimpleAnalyzerAssemblyLoader();

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            return Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(fullPath)));
        }
    }
}