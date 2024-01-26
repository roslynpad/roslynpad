using System;
using System.Threading.Tasks;

namespace RoslynPad.UI;

public abstract class TelemetryProviderBase : ITelemetryProvider
{
    private Exception? _lastError;

    public virtual void Initialize(string version, IApplicationSettings settings)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
    }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        HandleException(args.Exception!.Flatten().InnerException!);
    }

    private void CurrentDomainOnUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        HandleException((Exception)args.ExceptionObject);
    }

    protected void HandleException(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return;
        }

        LastError = exception;
    }

    public void ReportError(Exception exception)
    {
        HandleException(exception);
    }

    public Exception? LastError
    {
        get => _lastError;
        private set
        {
            _lastError = value;
            LastErrorChanged?.Invoke();
        }
    }

    public event Action? LastErrorChanged;

    public void ClearLastError()
    {
        LastError = null;
    }
}
