using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn;

public class DummyScriptMetadataResolver : MetadataReferenceResolver
{
    public static DummyScriptMetadataResolver Instance {  get; } = new DummyScriptMetadataResolver();

    private DummyScriptMetadataResolver() { }

    public override bool Equals(object? other) => ReferenceEquals(this, other);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    public override bool ResolveMissingAssemblies => false;

    public override PortableExecutableReference? ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity) => null;

    public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties) =>
        ImmutableArray<PortableExecutableReference>.Empty;
}
