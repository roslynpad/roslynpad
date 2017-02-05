using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RoslynPad.Annotations;
using RoslynPad.Roslyn.SymbolSearch;

namespace RoslynPad.UI
{
    [Export(typeof(ISymbolSearchService)), Export, Shared]
    internal class SymbolSearchService : ISymbolSearchService
    {
        private const int MaxResults = 3;

        public NuGetViewModel NuGet { get; set; }

        public async Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
        {
            var root = await Task.Run(() => GetRootObject(name, cancellationToken), cancellationToken).ConfigureAwait(false);

            return (
                from package in root.packages.Take(MaxResults)
                from type in package.match.typeNames
                select new PackageWithTypeResult(package.id, type.name, package.version, 1,
                    // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                    ImmutableArray<string>.Empty.Add(type._namespace))
            ).ToImmutableArray();
        }

        public Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray<PackageWithAssemblyResult>.Empty);
        }

        public Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty);
        }

        private static async Task<RootObject> GetRootObject(string name, CancellationToken cancellationToken)
        {
            var uri = "http://resharper-nugetsearch.jetbrains.com/api/v1/" +
                      $"find-type?name={name}&allowPrerelease=true";

            RootObject root;
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
                var stream = await result.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using (var streamReader = new StreamReader(stream))
                using (var reader = new JsonTextReader(streamReader))
                {
                    root = new JsonSerializer().Deserialize<RootObject>(reader);
                }
            }
            return root;
        }

        // ReSharper disable InconsistentNaming
        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local

        [UsedImplicitly]
        private class RootObject
        {
            public Package[] packages { get; set; }
        }

        [UsedImplicitly]
        private class Package
        {
            public string id { get; set; }
            public string version { get; set; }
            public Match match { get; set; }
        }

        [UsedImplicitly]
        private class Match
        {
            public Typename[] typeNames { get; set; }
        }

        [UsedImplicitly]
        private class Typename
        {
            public string _namespace { get; set; }
            public string name { get; set; }
        }
        
        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedMember.Local
        // ReSharper restore UnusedAutoPropertyAccessor.Local
    }
}