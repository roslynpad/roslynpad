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
/// are rejected from the graph and logged instead of failing the whole composition.
/// </summary>
public static class EditorComposition
{
    /// <summary>
    /// The assemblies every composition includes: the Roslyn Workspaces/Features layers, the
    /// Morgania editor, the recompiled Roslyn EditorFeatures, and this assembly's editor-host
    /// services — a complete editor graph out of the box. Host applications add their own
    /// parts through <see cref="CreateExportProvider(IEnumerable{Assembly}?)"/>.
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

    public static ExportProvider CreateExportProvider(IEnumerable<Assembly>? additionalAssemblies = null)
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
        var configuration = CompositionConfiguration.Create(catalog);

        LogCompositionDiagnostics(parts, configuration);

        return configuration.CreateExportProviderFactory().CreateExportProvider();
    }

    private static void LogCompositionDiagnostics(DiscoveredParts parts, CompositionConfiguration configuration)
    {
        var log = new StringWriter();

        foreach (var error in parts.DiscoveryErrors)
        {
            log.WriteLine($"[discovery] {error}");
        }

        // The first level of the stack holds the root-cause errors; the rest are cascades.
        if (!configuration.CompositionErrors.IsEmpty)
        {
            foreach (var diagnostic in configuration.CompositionErrors.Peek())
            {
                log.WriteLine($"[rejected] {diagnostic.Message}");
            }
        }

        var text = log.ToString();
        if (text.Length > 0)
        {
            try
            {
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "composition.log"), text);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
