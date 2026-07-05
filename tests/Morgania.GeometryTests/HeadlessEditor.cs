using System.Composition;
using System.Composition.Hosting;
using System.Reflection;

using Avalonia;
using Avalonia.Headless;

using Microsoft.VisualStudio.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.GeometryTests;

/// <summary>
/// Headless Avalonia session (Skia text shaping, real font metrics) plus the editor
/// composition, shared by the geometry tests. Composition hosts must not be shared across
/// *failing* resolutions (ADR-003), but the geometry tests only resolve known-good
/// contracts, so one container is safe and fast.
/// </summary>
public sealed class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

internal static class HeadlessEditor
{
    private static readonly HeadlessUnitTestSession s_session = HeadlessUnitTestSession.StartNew(typeof(TestApp));
    private static CompositionHost? s_container;

    /// <summary>Runs <paramref name="action"/> on the Avalonia UI thread of the headless session.</summary>
    public static Task<TResult> RunAsync<TResult>(Func<TResult> action) =>
        s_session.Dispatch(action, CancellationToken.None);

    public static Task RunAsync(Action action) =>
        s_session.Dispatch(
            () =>
            {
                action();
                return true;
            },
            CancellationToken.None);

    public static CompositionHost Container => s_container ??= CreateContainer();

    /// <summary>Creates a view over the given text with an explicit viewport.</summary>
    public static IWpfTextView CreateView(string text, double width = 800.0, double height = 600.0, bool wordWrap = false)
    {
        var bufferFactory = Container.GetExport<ITextBufferFactoryService>();
        var contentTypes = Container.GetExport<IContentTypeRegistryService>();
        var editorFactory = Container.GetExport<ITextEditorFactoryService>();
        var buffer = bufferFactory.CreateTextBuffer(text, contentTypes.GetContentType("text"));
        var view = editorFactory.CreateTextView(buffer);
        if (wordWrap)
        {
            view.Options.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, WordWrapStyles.WordWrap | WordWrapStyles.VisibleGlyphs);
        }

        view.DisplayTextLineContainingBufferPosition(
            new SnapshotPoint(view.TextBuffer.CurrentSnapshot, 0),
            0.0,
            ViewRelativePosition.Top,
            width,
            height);
        return view;
    }

    /// <summary>
    /// Creates a fresh composition host over the standard editor catalog, the test
    /// assembly, and any extra assemblies (the extension-conformance suite composes the
    /// contract-only extension through here).
    /// </summary>
    public static CompositionHost CreateContainer(params Assembly[] extraAssemblies)
    {
        string[] assemblyNames =
        [
            // The abstractions assembly carries parts too (option/format definitions).
            "Morgania.Editor.Abstractions",
            "Morgania.Editor",
        ];

        return new ContainerConfiguration()
            .WithAssemblies(assemblyNames.Select(static name => Assembly.Load(name)))
            // The test assembly itself: [Export] parts defined by tests (and the host
            // stubs below) are discovered by the scan.
            .WithAssembly(typeof(HeadlessEditor).Assembly)
            .WithAssemblies(extraAssemblies)
            .CreateContainer();
    }

    [Shared]
    internal sealed class HostServices
    {
        private static readonly JoinableTaskContext s_joinableTaskContext = new();

        [Export]
        public JoinableTaskContext JoinableTaskContext => s_joinableTaskContext;
    }

    [Shared]
    [Export(typeof(ISmartIndentationService))]
    internal sealed class StubSmartIndentationService : ISmartIndentationService
    {
        public int? GetDesiredIndentation(ITextView textView, ITextSnapshotLine line) => null;
    }

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
