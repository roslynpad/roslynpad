using System;
using System.Composition;
using System.Configuration;
using System.Windows;
using System.Windows.Threading;
using Microsoft.ApplicationInsights;
using RoslynPad.UI;

namespace RoslynPad
{
    [Export(typeof(ITelemetryProvider)), Shared]
    internal class TelemetryProvider : TelemetryProviderBase
    {
        public override void Initialize(string version, IApplicationSettings settings)
        {
            base.Initialize(version, settings);

            Application.Current.DispatcherUnhandledException += OnUnhandledDispatcherException;
        }

        private void OnUnhandledDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            HandleException(args.Exception);
            args.Handled = true;
        }
        
        protected override string GetInstrumentationKey() => ConfigurationManager.AppSettings["InstrumentationKey"];
    }
}