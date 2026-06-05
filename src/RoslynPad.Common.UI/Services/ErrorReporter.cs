using System.Composition;
using System.Diagnostics;

namespace RoslynPad.UI;

[Export(typeof(IErrorReporter)), Shared]
[method: ImportingConstructor]
public class ErrorReporter(IAppDispatcher appDispatcher) : IErrorReporter
{
    public void Initialize(string version, IApplicationSettings settings)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        appDispatcher.UnhandledException += HandleException;
    }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        HandleException(args.Exception!.Flatten().InnerException!);
    }

    private void CurrentDomainOnUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        HandleException((Exception)args.ExceptionObject);
    }

    private void HandleException(Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            return;
        }

        LastError = exception;
        Debug.WriteLine(exception);
    }

    public void ReportError(Exception exception)
    {
        HandleException(exception);
    }

    public Exception? LastError
    {
        get;
        private set
        {
            field = value;
            LastErrorChanged?.Invoke();
        }
    }

    public event Action? LastErrorChanged;

    public void ClearLastError()
    {
        LastError = null;
    }
}
