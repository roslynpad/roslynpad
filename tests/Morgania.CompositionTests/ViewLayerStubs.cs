using System.Composition;
using Avalonia.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.CompositionTests;

/// <summary>
/// Minimal exports for host services the vendored core imports. The editor itself is real
/// (view layer, format maps, and the M6 IntelliSense presenters, including the tooltip
/// service, all live in Morgania.Editor); what remains stubbed here is what a host shell
/// would provide: smart indentation and the telemetry sink.
/// </summary>
internal static class ViewLayerStubs
{
    [Shared]
    [Export(typeof(ISmartIndentationService))]
    internal sealed class StubSmartIndentationService : ISmartIndentationService
    {
        public int? GetDesiredIndentation(ITextView textView, ITextSnapshotLine line) => null;
    }

    /// <summary>
    /// No-op telemetry sink; in VS this service is exported by the shell, and some parts
    /// (e.g. the command handler service factory) require it rather than AllowDefault it.
    /// </summary>
    [Shared]
    [Export(typeof(Microsoft.VisualStudio.Text.Utilities.ILoggingServiceInternal))]
    internal sealed class StubLoggingService : Microsoft.VisualStudio.Text.Utilities.ILoggingServiceInternal
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
