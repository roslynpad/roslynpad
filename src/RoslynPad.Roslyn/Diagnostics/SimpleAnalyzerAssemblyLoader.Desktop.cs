#nullable disable
#if !NETCOREAPP
using System;
using System.Composition;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Diagnostics
{
    [Export(typeof(IAnalyzerAssemblyLoader)), Shared]
    internal class SimpleAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        public static SimpleAnalyzerAssemblyLoader Instance { get; } = new();

        private int _hookedAssemblyResolve;

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            if (Interlocked.CompareExchange(ref _hookedAssemblyResolve, 0, 1) == 0)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }

            return LoadImpl(fullPath);
        }

        protected virtual Assembly LoadImpl(string fullPath) => Assembly.LoadFrom(fullPath);

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Load(AppDomain.CurrentDomain.ApplyPolicy(args.Name));
            }
            catch
            {
                return null!;
            }
        }
    }
}
#endif
