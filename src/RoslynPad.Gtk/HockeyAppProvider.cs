using System;
using System.Composition;
using System.Threading.Tasks;
using RoslynPad.UI;

namespace RoslynPad.Gtk
{
    [Export(typeof(ITelemetryProvider))]
    public class HockeyAppProvider : ITelemetryProvider
    {
        public void Initialize(string currentVersion)
        {
        }

        public Exception LastError { get; } = null;

        public event Action LastErrorChanged;

        public void ClearLastError()
        {
        }

        public Task SubmitFeedback(string feedbackText, string email)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnLastErrorChanged()
        {
            LastErrorChanged?.Invoke();
        }
    }
}