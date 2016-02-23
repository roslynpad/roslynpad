using System;
using System.Collections.Immutable;
using System.Reflection;

namespace RoslynPad.Roslyn.Diagnostics
{
    internal static class HostDiagnosticAnalyzerPackageFactory
    {
        internal static readonly Type Type = Type.GetType("Microsoft.CodeAnalysis.Diagnostics.HostDiagnosticAnalyzerPackage, Microsoft.CodeAnalysis.Features", throwOnError: true);
        private static readonly ConstructorInfo Constructor = Type.GetConstructor(new[] { typeof(string), typeof(ImmutableArray<string>) });

        public static object Create(string name, ImmutableArray<string> assemblies)
        {
            if (Constructor == null)
            {
                throw new InvalidOperationException("Missing ctor");
            }
            return Constructor.Invoke(new object[] { name, assemblies });
        }
    }
}