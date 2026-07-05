using System.Composition;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Threading;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>Services the editor core and Roslyn import from the host application.</summary>
[Shared]
public sealed class HostServiceExports
{
    private static JoinableTaskContext? s_joinableTaskContext;

    /// <summary>
    /// Binds the <see cref="JoinableTaskContext"/> to the UI thread. Must be called from the
    /// UI thread before the Roslyn host composes; the composition itself may run on a
    /// background thread (MainViewModel builds the host in Task.Run), so the context cannot
    /// be created lazily at resolve time.
    /// </summary>
    public static void InitializeMainThread()
    {
        Avalonia.Threading.AvaloniaSynchronizationContext.InstallIfNeeded();
        s_joinableTaskContext = new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
    }

    [Export]
    public JoinableTaskContext JoinableTaskContext => s_joinableTaskContext
        ?? throw new InvalidOperationException($"{nameof(HostServiceExports)}.{nameof(InitializeMainThread)} was not called on the UI thread");
}

[Shared]
[Export(typeof(ILoggingServiceInternal))]
public sealed class HostLoggingService : ILoggingServiceInternal
{
    public void PostEvent(string key, params object[] namesAndProperties)
    {
    }

    public void PostEvent(string key, IReadOnlyList<object> namesAndProperties)
    {
    }

    public void PostEvent(TelemetryEventType eventType, string eventName, TelemetryResult result = TelemetryResult.Success, params (string name, object property)[] namesAndProperties)
    {
    }

    public void PostEvent(TelemetryEventType eventType, string eventName, TelemetryResult result, IReadOnlyList<(string name, object property)> namesAndProperties)
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

    public object? CreateTelemetryOperationEventScope(string eventName, TelemetrySeverity severity, object[] correlations, IDictionary<string, object> startingProperties) => null;

    public object? GetCorrelationFromTelemetryScope(object telemetryScope) => null;

    public void EndTelemetryScope(object telemetryScope, TelemetryResult result, string? summary = null)
    {
    }
}
