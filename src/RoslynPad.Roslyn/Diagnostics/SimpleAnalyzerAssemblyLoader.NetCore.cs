#nullable disable
#if NETCOREAPP
using System.Composition;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Diagnostics
{
    [Export(typeof(IAnalyzerAssemblyLoader)), Shared]
    internal class SimpleAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        public static SimpleAnalyzerAssemblyLoader Instance { get; } = new();

        private AssemblyLoadContext _loadContext;

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            if (_loadContext == null)
            {
                AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(typeof(DefaultAnalyzerAssemblyLoader).GetTypeInfo().Assembly);

                if (System.Threading.Interlocked.CompareExchange(ref _loadContext, loadContext, null) == null)
                {
                    _loadContext.Resolving += (context, name) =>
                    {
                        Debug.Assert(ReferenceEquals(context, _loadContext));
                        return Load(name.FullName);
                    };
                }
            }

            return LoadImpl(fullPath);
        }

        protected virtual Assembly LoadImpl(string fullPath) => _loadContext.LoadFromAssemblyPath(fullPath);
    }
}
#endif
