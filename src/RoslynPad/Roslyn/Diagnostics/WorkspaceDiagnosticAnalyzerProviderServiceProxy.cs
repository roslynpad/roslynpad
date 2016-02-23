using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Castle.Core.Interceptor;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Diagnostics
{
    internal sealed class WorkspaceDiagnosticAnalyzerProviderServiceProxy : IInterceptor
    {
        internal static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.Diagnostics.IWorkspaceDiagnosticAnalyzerProviderService, Microsoft.CodeAnalysis.Features", throwOnError: true);

        internal static readonly Lazy<Type> GeneratedType = new Lazy<Type>(() => RoslynInterfaceProxy.GenerateFor(InterfaceType, isWorkspaceService: false));

        void IInterceptor.Intercept(IInvocation invocation)
        {
            switch (invocation.Method.Name)
            {
                case "GetHostDiagnosticAnalyzerPackages":
                    var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var array = Array.CreateInstance(HostDiagnosticAnalyzerPackageFactory.Type, 1);
                    Debug.Assert(path != null, "path != null");
                    array.SetValue(HostDiagnosticAnalyzerPackageFactory.Create(LanguageNames.CSharp,
                        ImmutableArray.Create(
                            Path.Combine(path, "Microsoft.CodeAnalysis.dll"),
                            Path.Combine(path, "Microsoft.CodeAnalysis.CSharp.dll"))), 0);
                    invocation.ReturnValue = array;
                    break;
                case "GetAnalyzerAssemblyLoader":
                    invocation.ReturnValue = new AnalyzerAssemblyLoader();
                    break;
            }
        }
    }
}