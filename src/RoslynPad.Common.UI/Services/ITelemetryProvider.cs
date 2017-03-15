using System;
using System.Threading.Tasks;

namespace RoslynPad.UI
{
    public interface ITelemetryProvider
    {
        void Initialize(string currentVersion);
        Exception LastError { get; }
        event Action LastErrorChanged;
        void ClearLastError();
        Task SubmitFeedback(string feedbackText, string email);
        void ReportError(Exception exception);
    }
}