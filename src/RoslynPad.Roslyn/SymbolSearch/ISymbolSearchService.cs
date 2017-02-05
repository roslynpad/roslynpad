using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.SymbolSearch
{
    [ExportWorkspaceService(typeof(Microsoft.CodeAnalysis.SymbolSearch.ISymbolSearchService), ServiceLayer.Host), Shared]
    internal class DefaultSymbolSearchService : Microsoft.CodeAnalysis.SymbolSearch.ISymbolSearchService
    {
        private readonly ISymbolSearchService _implementation;

        [ImportingConstructor]
        public DefaultSymbolSearchService(ISymbolSearchService implementation)
        {
            _implementation = implementation;
        }

        public async Task<ImmutableArray<Microsoft.CodeAnalysis.SymbolSearch.PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
        {
            var result = await _implementation.FindPackagesWithTypeAsync(source, name, arity, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(x => new Microsoft.CodeAnalysis.SymbolSearch.PackageWithTypeResult(
                x.PackageName, x.TypeName, x.Version, x.Rank, x.ContainingNamespaceNames));
        }

        public async Task<ImmutableArray<Microsoft.CodeAnalysis.SymbolSearch.PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
        {
            var result = await _implementation.FindPackagesWithAssemblyAsync(source, assemblyName, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(x => new Microsoft.CodeAnalysis.SymbolSearch.PackageWithAssemblyResult(
                x.PackageName, x.Version, x.Rank));
        }

        public async Task<ImmutableArray<Microsoft.CodeAnalysis.SymbolSearch.ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
        {
            var result = await _implementation.FindReferenceAssembliesWithTypeAsync(name, arity, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(x => new Microsoft.CodeAnalysis.SymbolSearch.ReferenceAssemblyWithTypeResult(
                x.AssemblyName, x.TypeName, x.ContainingNamespaceNames));
        }
    }

    public interface ISymbolSearchService
    {
        Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
            string source, string name, int arity, CancellationToken cancellationToken);

        Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
            string source, string assemblyName, CancellationToken cancellationToken);

        Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
            string name, int arity, CancellationToken cancellationToken);
    }

    public abstract class PackageResult
    {
        public readonly string PackageName;
        internal readonly int Rank;

        protected PackageResult(string packageName, int rank)
        {
            PackageName = packageName;
            Rank = rank;
        }
    }

    public class PackageWithTypeResult : PackageResult
    {
        public readonly IReadOnlyList<string> ContainingNamespaceNames;
        public readonly string TypeName;
        public readonly string Version;

        public PackageWithTypeResult(
            string packageName,
            string typeName,
            string version,
            int rank,
            IReadOnlyList<string> containingNamespaceNames)
            : base(packageName, rank)
        {
            TypeName = typeName;
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
            ContainingNamespaceNames = containingNamespaceNames;
        }
    }

    public class PackageWithAssemblyResult : PackageResult, IEquatable<PackageWithAssemblyResult>, IComparable<PackageWithAssemblyResult>
    {
        public readonly string Version;

        public PackageWithAssemblyResult(
            string packageName,
            string version,
            int rank)
            : base(packageName, rank)
        {
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
        }

        public override int GetHashCode()
            => PackageName.GetHashCode();

        public override bool Equals(object obj)
            => Equals((PackageWithAssemblyResult)obj);

        public bool Equals(PackageWithAssemblyResult other)
            => PackageName.Equals(other?.PackageName);

        public int CompareTo(PackageWithAssemblyResult other)
        {
            var diff = Rank - other.Rank;
            if (diff != 0)
            {
                return -diff;
            }

            return string.Compare(PackageName, other.PackageName, StringComparison.Ordinal);
        }
    }

    public class ReferenceAssemblyWithTypeResult
    {
        public readonly IReadOnlyList<string> ContainingNamespaceNames;
        public readonly string AssemblyName;
        public readonly string TypeName;

        public ReferenceAssemblyWithTypeResult(
            string assemblyName,
            string typeName,
            IReadOnlyList<string> containingNamespaceNames)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
            ContainingNamespaceNames = containingNamespaceNames;
        }
    }
}