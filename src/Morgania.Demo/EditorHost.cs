namespace Microsoft.VisualStudio.Demo;

using System.Composition;
using System.Composition.Hosting;
using System.Reflection;

using Microsoft.VisualStudio.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

/// <summary>
/// The demo's composition host: the vendored editor core, the Morgania view layer, and this
/// assembly's language/host parts.
/// </summary>
public static class EditorHost
{
    private static readonly string[] AssemblyNames =
    [
        // The abstractions assembly carries parts too (option/format definitions).
        "Morgania.Editor.Abstractions",
        "Morgania.Editor",
    ];

    public static CompositionHost Create() =>
        new ContainerConfiguration()
            .WithAssemblies(AssemblyNames.Select(static name => Assembly.Load(name)))
            .WithAssembly(typeof(EditorHost).Assembly)
            .CreateContainer();

    /// <summary>Services the editor core imports from the host application.</summary>
    [Shared]
    public sealed class HostServices
    {
        private static JoinableTaskContext? s_joinableTaskContext;

        [Export]
        public JoinableTaskContext JoinableTaskContext => s_joinableTaskContext ??= CreateContext();

        // The brokers' SwitchToMainThreadAsync posts through the context's synchronization
        // context; bind it to the Avalonia dispatcher explicitly (the container composes on
        // the UI thread, but before the run loop installs its own context).
        private static JoinableTaskContext CreateContext()
        {
            Avalonia.Threading.AvaloniaSynchronizationContext.InstallIfNeeded();
            return new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
        }
    }

    [Shared]
    [Export(typeof(ISmartIndentationService))]
    public sealed class HostSmartIndentationService : ISmartIndentationService
    {
        public int? GetDesiredIndentation(ITextView textView, ITextSnapshotLine line) => null;
    }

    [Shared]
    [Export(typeof(Microsoft.VisualStudio.Text.Utilities.ILoggingServiceInternal))]
    public sealed class HostLoggingService : Microsoft.VisualStudio.Text.Utilities.ILoggingServiceInternal
    {
        public void PostEvent(string key, params object[] namesAndProperties)
        {
        }

        public void PostEvent(string key, IReadOnlyList<object> namesAndProperties)
        {
        }

        public void PostEvent(Microsoft.VisualStudio.Text.Utilities.TelemetryEventType eventType, string eventName, Microsoft.VisualStudio.Text.Utilities.TelemetryResult result = Microsoft.VisualStudio.Text.Utilities.TelemetryResult.Success, params (string name, object property)[] namesAndProperties)
        {
        }

        public void PostEvent(Microsoft.VisualStudio.Text.Utilities.TelemetryEventType eventType, string eventName, Microsoft.VisualStudio.Text.Utilities.TelemetryResult result, IReadOnlyList<(string name, object property)> namesAndProperties)
        {
        }

        public void PostFault(string eventName, string description, Exception exceptionObject, string? additionalErrorInfo = null, bool? isIncludedInWatsonSample = null, object[]? correlations = null)
        {
        }

        public void AdjustCounter(string key, string name, int delta = 1)
        {
        }

        public void PostCounters()
        {
        }

        public object? CreateTelemetryOperationEventScope(string eventName, Microsoft.VisualStudio.Text.Utilities.TelemetrySeverity severity, object[] correlations, IDictionary<string, object> startingProperties) => null;

        public object? GetCorrelationFromTelemetryScope(object telemetryScope) => null;

        public void EndTelemetryScope(object telemetryScope, Microsoft.VisualStudio.Text.Utilities.TelemetryResult result, string? summary = null)
        {
        }
    }
}
