using System;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;

namespace RoslynPad.UI;

public abstract class TelemetryProviderBase : ITelemetryProvider
{
    private Exception? _lastError;
    private ILoggerFactory? _loggerFactory;
    private ILogger? _logger;

    public virtual void Initialize(string version, IApplicationSettings settings)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

        if (!settings.Values.SendErrors ||
            (GetInstrumentationKey() is var instrumentationKey && string.IsNullOrEmpty(instrumentationKey)))
        {
            return;
        }

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddOpenTelemetry(options =>
                options.AddAzureMonitorLogExporter(o => o.ConnectionString = "InstrumentationKey=" + instrumentationKey));
        });

        _logger = _loggerFactory.CreateLogger(nameof(RoslynPad));
        _logger.LogInformation(nameof(Initialize));
    }

    protected abstract string? GetInstrumentationKey();

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

        _logger?.LogError(exception, exception.Message);
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
