using System.Composition;
using RoslynPad.UI;

namespace RoslynPad
{
    [Export(typeof(ITelemetryProvider)), Shared]
    internal class TelemetryProvider : TelemetryProviderBase
    {
        public override void Initialize(string version, IApplicationSettings settings)
        {
            base.Initialize(version, settings);

            // TODO
            // Application.Current.DispatcherUnhandledException += OnUnhandledDispatcherException;
        }

        //private void OnUnhandledDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
        //{
        //    HandleException(args.Exception);
        //    args.Handled = true;
        //}
        
        // TODO
        protected override string GetInstrumentationKey() => string.Empty;
    }
}