using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn;

public class RoslynHostReferences
{
    public static RoslynHostReferences Empty { get; } = new(
        ImmutableArray<MetadataReference>.Empty,
        ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase),
        ImmutableArray<string>.Empty);

    /// <summary>
    /// Returns namespace-only (no assemblies) defaults that fit all frameworks.
    /// </summary>
    public static RoslynHostReferences NamespaceDefault { get; } = Empty.With(imports: new[]{
        "System",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Collections",
        "System.Collections.Generic",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Linq",
        "System.IO",
        "System.Reflection",
    });

    public RoslynHostReferences With(IEnumerable<MetadataReference>? references = null, IEnumerable<string>? imports = null,
        IEnumerable<Assembly>? assemblyReferences = null, IEnumerable<string>? assemblyPathReferences = null, IEnumerable<Type>? typeNamespaceImports = null)
    {
        var referenceLocations = _referenceLocations;
        var importsArray = Imports.AddRange(imports!.WhereNotNull());

        var locations =
            assemblyReferences!.WhereNotNull().Select(c => c.Location).Concat(
            assemblyPathReferences!.WhereNotNull());

        foreach (var location in locations)
        {
            referenceLocations = referenceLocations.SetItem(location, string.Empty);
        }

        foreach (var type in typeNamespaceImports!.WhereNotNull())
        {
            importsArray = importsArray.Add(type!.Namespace!);
            var location = type.Assembly.Location;
            referenceLocations = referenceLocations.SetItem(location, string.Empty);
        }

        return new RoslynHostReferences(
            _references.AddRange(references!.WhereNotNull()),
            referenceLocations,
            importsArray);
    }

    private RoslynHostReferences(
        ImmutableArray<MetadataReference> references,
        ImmutableDictionary<string, string> referenceLocations,
        ImmutableArray<string> imports)
    {
        _references = references;
        _referenceLocations = referenceLocations;
        Imports = imports;
    }

    private readonly ImmutableArray<MetadataReference> _references;

    private readonly ImmutableDictionary<string, string> _referenceLocations;

    public ImmutableArray<string> Imports { get; }

    public ImmutableArray<MetadataReference> GetReferences(Func<string, DocumentationProvider>? documentationProviderFactory = null) =>
        Enumerable.Concat(_references, Enumerable.Select(_referenceLocations, c => MetadataReference.CreateFromFile(c.Key, documentation: documentationProviderFactory?.Invoke(c.Key))))
            .ToImmutableArray();
}
