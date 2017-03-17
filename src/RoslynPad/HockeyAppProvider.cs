using System;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.HockeyApp;
using RoslynPad.UI;

namespace RoslynPad
{
    [Export(typeof(ITelemetryProvider)), Shared]
    internal class HockeyAppProvider : ITelemetryProvider
    {
        private Exception _lastError;
        private const string HockeyAppId = "8655168826d9412483763f7ddcf84b8e";

        public void Initialize(string currentVersion, IApplicationSettings settings)
        {
            var hockeyClient = (HockeyClient)HockeyClient.Current;
            if (settings.SendErrors)
            {
                hockeyClient.Configure(HockeyAppId)
                    .RegisterCustomDispatcherUnhandledExceptionLogic(OnUnhandledDispatcherException)
                    .UnregisterDefaultUnobservedTaskExceptionHandler();

                var platformHelper = (HockeyPlatformHelperWPF)hockeyClient.PlatformHelper;
                platformHelper.AppVersion = currentVersion;

#if DEBUG
                hockeyClient.OnHockeySDKInternalException += (sender, args) =>
                {
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
                };
#endif

                var task = HockeyAppWorkaroundInitializer.InitializeAsync();
                task.ContinueWith(t =>
                {
                    Debug.Assert(hockeyClient.IsTelemetryInitialized, "hockeyClient.IsTelemetryInitialized");
                    hockeyClient.TrackEvent(TelemetryEventNames.Start);
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                Application.Current.DispatcherUnhandledException +=
                    (sender, args) => OnUnhandledDispatcherException(args);

                var platformHelper = new HockeyPlatformHelperWPF { AppVersion = currentVersion };
                hockeyClient.PlatformHelper = platformHelper;
                hockeyClient.AppIdentifier = HockeyAppId;
            }
        }

        private void OnUnhandledDispatcherException(DispatcherUnhandledExceptionEventArgs args)
        {
            var exception = args.Exception;
            if (exception is OperationCanceledException)
            {
                args.Handled = true;
                return;
            }
            LastError = exception;
            args.Handled = true;
        }

        public Task SubmitFeedback(string feedbackText, string email)
        {
            return Task.Run(async () =>
            {
                var feedback = HockeyClient.Current.CreateFeedbackThread();
                await feedback.PostFeedbackMessageAsync(feedbackText, email).ConfigureAwait(false);
            });
        }

        public void ReportError(Exception exception)
        {
            HockeyClient.Current.TrackException(exception);
            LastError = exception;
        }

        public Exception LastError
        {
            get => _lastError; private set
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

        private static class TelemetryEventNames
        {
            public const string Start = "Start";
        }
    }
}