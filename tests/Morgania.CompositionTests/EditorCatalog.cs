using System.Composition;
using System.Composition.Hosting;
using System.Reflection;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.CompositionTests;

/// <summary>
/// Builds the standard editor composition: every vendored assembly plus the host-provided
/// services that the VS shell would normally contribute.
/// </summary>
internal static class EditorCatalog
{
    /// <summary>
    /// The editor assemblies, by assembly name. The abstractions assembly is included
    /// too: a few contracts carry parts (e.g. option/format definitions).
    /// </summary>
    public static readonly string[] AssemblyNames =
    [
        "Morgania.Editor.Abstractions",
        "Morgania.Editor",
    ];

    public static Assembly[] LoadAssemblies() =>
        [.. AssemblyNames.Select(static name => Assembly.Load(name))];

    public static CompositionHost CreateHost() =>
        new ContainerConfiguration()
            .WithAssemblies(LoadAssemblies())
            .WithPart<HostServices>()
            .WithPart<ViewLayerStubs.StubSmartIndentationService>()
            .WithPart<ViewLayerStubs.StubLoggingService>()
            .CreateContainer();

    /// <summary>
    /// Services the editor core imports but expects the host application to export.
    /// </summary>
    [Shared]
    internal sealed class HostServices
    {
        private static readonly JoinableTaskContext s_joinableTaskContext = new();

        [Export]
        public JoinableTaskContext JoinableTaskContext => s_joinableTaskContext;
    }
}
