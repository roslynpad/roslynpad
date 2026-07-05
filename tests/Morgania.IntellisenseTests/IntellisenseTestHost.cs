using System.Composition;
using System.Composition.Hosting;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

using Microsoft.VisualStudio.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.IntellisenseTests;

/// <summary>
/// Headless session + composition for the IntelliSense acceptance suite: the standard editor
/// catalog plus the real presenter layer (Morgania.Editor's Intellisense/) — no tooltip stub here, the
/// vendored Quick Info broker gets the real <c>IToolTipService</c> — plus this assembly's
/// fake language service.
/// </summary>
public sealed class TestApp : Application
{
    public override void Initialize()
    {
        // Popups host in the window's OverlayLayer, which only exists when the window has
        // a templated visual tree — i.e. a theme.
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

internal static class IntellisenseTestHost
{
    private static readonly HeadlessUnitTestSession s_session = HeadlessUnitTestSession.StartNew(typeof(TestApp));
    private static CompositionHost? s_container;

    static IntellisenseTestHost()
    {
        // The vendored Quick Info session Debug.Asserts that its background computation is
        // off the JTC main thread. The headless session's UI thread IS a thread-pool
        // thread, so vs-threading's `await TaskScheduler.Default` completes inline there
        // and the assert fires — a false positive of the test environment (nothing blocks,
        // so inline execution is safe). Keep the assert from fail-fasting the test host.
        System.Diagnostics.Trace.Listeners.Clear();
        System.Diagnostics.Trace.Listeners.Add(new NonFatalAssertListener());
    }

    private sealed class NonFatalAssertListener : System.Diagnostics.TraceListener
    {
        public override void Fail(string? message, string? detailMessage)
            => Console.WriteLine($"Debug.Assert (non-fatal in tests): {message} {detailMessage}");

        public override void Write(string? message) => Console.Write(message);

        public override void WriteLine(string? message) => Console.WriteLine(message);
    }

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

    /// <summary>
    /// Runs an async body on the session's UI thread. Awaits inside the body yield back to
    /// the session's dispatcher loop, which lets <see cref="Avalonia.Threading.DispatcherTimer"/>s
    /// fire — required by anything driven by real timers (e.g. the mouse-hover cycle).
    /// The wrapper must return a value: only the <c>Func&lt;Task&lt;TResult&gt;&gt;</c>
    /// Dispatch overload awaits the inner task (a bare <c>Func&lt;Task&gt;</c> binds to the
    /// synchronous overload and abandons the body at its first await).
    /// </summary>
    public static Task RunAsync(Func<Task> action) =>
        s_session.Dispatch(
            async () =>
            {
                await action().ConfigureAwait(true);
                return true;
            },
            CancellationToken.None);

    public static CompositionHost Container => s_container ??= CreateContainer();

    /// <summary>
    /// A view over the given text hosted in a shown headless window (popups need a
    /// top-level with an overlay layer), with the initial layout run.
    /// </summary>
    public static (IWpfTextView View, Window Window) CreateHostedView(string text, double width = 800.0, double height = 600.0)
    {
        var bufferFactory = Container.GetExport<ITextBufferFactoryService>();
        var contentTypes = Container.GetExport<IContentTypeRegistryService>();
        var editorFactory = Container.GetExport<ITextEditorFactoryService>();
        var buffer = bufferFactory.CreateTextBuffer(text, contentTypes.GetContentType("text"));
        var view = editorFactory.CreateTextView(buffer);
        var host = editorFactory.CreateTextViewHost(view, setFocus: false);
        var window = new Window { Width = width, Height = height, Content = host.HostControl };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (view, window);
    }

    private static CompositionHost CreateContainer()
    {
        string[] assemblyNames =
        [
            // The abstractions assembly carries parts too (option/format definitions).
            "Morgania.Editor.Abstractions",
            // Includes the view layer and the IntelliSense presenter layer.
            "Morgania.Editor",
        ];

        return new ContainerConfiguration()
            .WithAssemblies(assemblyNames.Select(static name => Assembly.Load(name)))
            .WithAssembly(typeof(IntellisenseTestHost).Assembly)
            .CreateContainer();
    }

    [Shared]
    internal sealed class HostServices
    {
        private static JoinableTaskContext? s_joinableTaskContext;

        [Export]
        public JoinableTaskContext JoinableTaskContext => s_joinableTaskContext ??= CreateContext();

        // The context must bind to the headless UI thread with a synchronization context
        // that posts through the Avalonia dispatcher — SwitchToMainThreadAsync inside the
        // vendored brokers rides on it. Resolution happens on the session's UI thread.
        private static JoinableTaskContext CreateContext()
        {
            Avalonia.Threading.AvaloniaSynchronizationContext.InstallIfNeeded();
            return new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current);
        }
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
