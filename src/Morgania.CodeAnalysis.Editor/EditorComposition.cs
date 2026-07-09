using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Composition;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Builds the single VS-MEF graph shared by Roslyn and the editor. VS MEF
/// (Microsoft.VisualStudio.Composition) is required because the graph mixes attribute
/// flavors: Roslyn's Workspaces/Features packages and the Morgania editor are MEF v2
/// (System.Composition), while the recompiled EditorFeatures assemblies export their
/// editor-facing parts with v1 attributes (System.ComponentModel.Composition) — exactly
/// like in Visual Studio itself. Parts with imports that cannot be satisfied outside VS
/// are rejected from the graph instead of failing the whole composition; the returned
/// <see cref="CompositionConfiguration"/> reports them.
/// </summary>
public static class EditorComposition
{
    /// <summary>
    /// The assemblies every composition includes: the Roslyn Workspaces/Features layers, the
    /// Morgania editor, the recompiled Roslyn EditorFeatures, and this assembly's editor-host
    /// services — a complete editor graph out of the box. Host applications add their own
    /// parts through <see cref="CreateConfiguration(IEnumerable{Assembly}?)"/>.
    /// </summary>
    public static ImmutableArray<Assembly> DefaultAssemblies { get; } =
        [
            typeof(WorkspacesResources).Assembly,
            typeof(CSharpWorkspaceResources).Assembly,
            typeof(FeaturesResources).Assembly,
            typeof(CSharpFeaturesResources).Assembly,
            // Morgania.Editor.Abstractions
            typeof(Microsoft.VisualStudio.Text.ITextBuffer).Assembly,
            // Morgania.Editor
            typeof(Microsoft.VisualStudio.Text.Classification.Implementation.ClassificationFormatMapService).Assembly,
            // Morgania.CodeAnalysis.EditorFeatures
            typeof(EditorFeaturesResources).Assembly,
            typeof(EditorComposition).Assembly,
        ];

    /// <summary>
    /// Discovers parts in <see cref="DefaultAssemblies"/> (plus
    /// <paramref name="additionalAssemblies"/>) and composes them. Call
    /// <see cref="CompositionConfiguration.CreateExportProviderFactory"/> on the result to
    /// obtain an <see cref="ExportProvider"/>. Diagnostics are the caller's decision:
    /// <see cref="CompositionConfiguration.CompositionErrors"/> lists rejected parts and
    /// <c>Catalog.DiscoveredParts.DiscoveryErrors</c> lists discovery failures. Note that a
    /// working graph still reports rejections — EditorFeatures parts with VS-only imports
    /// are rejected by design — so treat them as advisory rather than calling
    /// <see cref="CompositionConfiguration.ThrowOnErrors"/>.
    /// </summary>
    public static CompositionConfiguration CreateConfiguration(IEnumerable<Assembly>? additionalAssemblies = null)
    {
        var assemblies = DefaultAssemblies;

        if (additionalAssemblies != null)
        {
            assemblies = [.. assemblies.Concat(additionalAssemblies).Distinct()];
        }

        var discovery = PartDiscovery.Combine(
            new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true),
            new AttributedPartDiscoveryV1(Resolver.DefaultInstance));

        var parts = Task.Run(() => discovery.CreatePartsAsync(assemblies)).GetAwaiter().GetResult();
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts);
        return CompositionConfiguration.Create(catalog);
    }
}
