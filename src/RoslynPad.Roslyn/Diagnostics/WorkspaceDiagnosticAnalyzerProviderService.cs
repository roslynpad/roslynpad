using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RoslynPad.Roslyn.Diagnostics
{
    [Export(typeof(IWorkspaceDiagnosticAnalyzerProviderService))]
    internal sealed class WorkspaceDiagnosticAnalyzerProviderService : IWorkspaceDiagnosticAnalyzerProviderService
    {
        public IEnumerable<HostDiagnosticAnalyzerPackage> GetHostDiagnosticAnalyzerPackages()
        {
            var path = Path.GetDirectoryName(typeof(WorkspaceDiagnosticAnalyzerProviderService).GetTypeInfo().Assembly.GetLocation());
            if (path == null) throw new ArgumentNullException(nameof(path));
            return new[]
            {
                new HostDiagnosticAnalyzerPackage(LanguageNames.CSharp,
                    ImmutableArray.Create(
                        Path.Combine(path, "Microsoft.CodeAnalysis.dll"),
                        Path.Combine(path, "Microsoft.CodeAnalysis.CSharp.dll"),
                        Path.Combine(path, "Microsoft.CodeAnalysis.Features.dll"),
                        Path.Combine(path, "Microsoft.CodeAnalysis.CSharp.Features.dll")))
            };
        }

        public IAnalyzerAssemblyLoader GetAnalyzerAssemblyLoader()
        {
            return new AnalyzerAssemblyLoader();
        }
    }
}