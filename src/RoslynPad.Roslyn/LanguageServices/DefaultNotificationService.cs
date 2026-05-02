using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;

namespace RoslynPad.Roslyn.LanguageServices;

[ExportWorkspaceService(typeof(INotificationService)), Shared]
internal class DefaultNotificationService : INotificationService
{
    public bool ConfirmMessageBox(string message, string? title = null, NotificationSeverity severity = NotificationSeverity.Warning) => true;
    public void SendNotification(string message, string? title = null, NotificationSeverity severity = NotificationSeverity.Warning) { }
}
