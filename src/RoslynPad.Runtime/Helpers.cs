using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RoslynPad.Runtime;

/// <summary>
/// RoslynPad runtime helpers.
/// </summary>
public static class Helpers
{
    internal static event Action<double?>? Progress;

    private static readonly Lazy<Task<SynchronizationContext>> s_dispatcherTask = new(CreateWpfDispatcherAsync);

    /// <summary>
    /// Creates a new thread running a WPF Dispatcher and returns a <see cref="SynchronizationContext"/> for that dispatcher.
    /// </summary>
    /// <returns></returns>
    public static async Task<SynchronizationContext> CreateWpfDispatcherAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException($"{nameof(CreateWpfDispatcherAsync)} is supported only on Windows");
        }

        var windowsBaseAssembly = Assembly.Load("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
        var dispatcherType = windowsBaseAssembly.GetType("System.Windows.Threading.Dispatcher", throwOnError: true) ?? throw new InvalidOperationException();
        var dispatcherSyncContextCtor = windowsBaseAssembly.GetType("System.Windows.Threading.DispatcherSynchronizationContext", throwOnError: true)!
            .GetConstructors().FirstOrDefault(c => c.GetParameters() is var p && p.Length == 1 && p[0].ParameterType.Name == "Dispatcher") ?? throw new InvalidOperationException();
        var runMethod = dispatcherType.GetMethod("Run", [])?.CreateDelegate(typeof(Action)) as Action ?? throw new InvalidOperationException();
        var currentDispatcherProperty = dispatcherType.GetProperty("CurrentDispatcher") ?? throw new InvalidOperationException();

        var tcs = new TaskCompletionSource<object>();

        var thread = new Thread(() =>
        {
            var dispatcher = currentDispatcherProperty.GetValue(null) ?? throw new InvalidOperationException();
            tcs.SetResult(dispatcher);
            runMethod();
        });

        thread.SetApartmentState(ApartmentState.STA);

        thread.IsBackground = true;
        thread.Start();

        var dispatcher = await tcs.Task.ConfigureAwait(false);

        return (SynchronizationContext)dispatcherSyncContextCtor.Invoke([dispatcher]);
    }

    /// <summary>
    /// Await this method to yield to a default WPF Dispatcher thread.
    /// A Dispatcher creates a message pump that can be used with most Windows UI frameworks (e.g. WPF, Windows Forms).
    /// Lazily creates a Dispatcher using <see cref="CreateWpfDispatcherAsync"/>).
    /// </summary>
    public static SynchronizationContextAwaitable RunWpfAsync() => new(s_dispatcherTask.Value);

    /// <summary>
    /// Reports progress to the UI
    /// </summary>
    /// <param name="progress">Progress between 0.0 and 1.0 or null to hide progress report</param>
    public static void ReportProgress(double? progress)
    {
        if (progress.HasValue)
        {
            if (progress.Value < 0.0)
            {
                progress = 0.0;
            }
            else if (progress.Value > 1.0)
            {
                progress = 1.0;
            }
        }

        Progress?.Invoke(progress);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct SynchronizationContextAwaitable(Task<SynchronizationContext> task)
    {
        private readonly Task<SynchronizationContext> _task = task;

        public SynchronizationContextAwaiter GetAwaiter() => new(_task);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct SynchronizationContextAwaiter(Task<SynchronizationContext> task) : INotifyCompletion
    {
        private static readonly SendOrPostCallback s_postCallback = state => ((Action)state!)();

        private readonly Task<SynchronizationContext> _task = task;

        public bool IsCompleted => false;

        public void OnCompleted(Action continuation)
        {
            if (_task.Status == TaskStatus.RanToCompletion)
            {
                _task.Result.Post(s_postCallback, continuation);
                return;
            }

            _task.ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    t.Result.Post(s_postCallback, continuation);
                }
                else
                {
                    // GetResult will throw
                    continuation();
                }
            });
        }

        public void GetResult() => _task.GetAwaiter().GetResult();
    }
}
