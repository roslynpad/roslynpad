using System;
using System.Composition;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.ApplicationInsights;
using RoslynPad.UI;

namespace RoslynPad
{
    [Export(typeof(ITelemetryProvider)), Shared]
    internal class TelemetryProvider : ITelemetryProvider
    {
        private TelemetryClient _client;
        private Exception _lastError;

        public void Initialize(string version, IApplicationSettings settings)
        {
            if (settings.SendErrors)
            {
                var instrumentationKey = ConfigurationManager.AppSettings["InstrumentationKey"];

                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    _client = new TelemetryClient
                    {
                        InstrumentationKey = instrumentationKey
                    };

                    _client.Context.Component.Version = version;

                    _client.TrackPageView("Main");
                }
            }

            Application.Current.DispatcherUnhandledException += OnUnhandledDispatcherException;
            if (_client != null)
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            }
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            HandleException(args.Exception.Flatten().InnerException);
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            HandleException((Exception)args.ExceptionObject);
            _client?.Flush();
        }

        private void OnUnhandledDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            HandleException(args.Exception);
            args.Handled = true;
        }

        private void HandleException(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return;
            }

            _client?.TrackException(exception);
            LastError = exception;
        }

        public void ReportError(Exception exception)
        {
            HandleException(exception);
        }

        public Exception LastError
        {
            get => _lastError;
            private set
            {
                _lastError = value;
                LastErrorChanged?.Invoke();
            }
        }

        public event Action LastErrorChanged;

        public void ClearLastError()
        {
            LastError = null;
        }
    }
}