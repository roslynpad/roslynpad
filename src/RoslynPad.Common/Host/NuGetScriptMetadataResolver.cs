using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using RoslynPad.Roslyn;

namespace RoslynPad.Host
{
    internal sealed class NuGetScriptMetadataResolver : MetadataReferenceResolver
    {
        private readonly INuGetProvider _nuGetProvider;
        private readonly ScriptMetadataResolver _inner;

        public NuGetScriptMetadataResolver(INuGetProvider nuGetProvider, string workingDirectory)
        {
            _nuGetProvider = nuGetProvider;
            _inner = ScriptMetadataResolver.Default.WithBaseDirectory(workingDirectory);
        }

        public override bool Equals(object other)
        {
            return _inner.Equals(other);
        }

        public override int GetHashCode()
        {
            return _inner.GetHashCode();
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            reference = _nuGetProvider.ResolveReference(reference);
            return _inner.ResolveReference(reference, baseFilePath, properties);
        }
    }
}