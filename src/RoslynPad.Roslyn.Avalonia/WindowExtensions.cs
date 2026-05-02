using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace RoslynPad.Roslyn;

internal static class WindowExtensions
{
    public static bool? ShowDialogSync(this Window dialog)
    {
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?
            .Windows.FirstOrDefault(w => w.IsActive);
        if (owner == null) return false;

        bool? result = null;
        var frame = new DispatcherFrame();

        var task = dialog.ShowDialog<bool?>(owner);
        task.ContinueWith(t =>
        {
            result = t.IsCompletedSuccessfully ? t.Result : false;
            frame.Continue = false;
        }, TaskScheduler.FromCurrentSynchronizationContext());

        if (!task.IsCompleted)
        {
            Dispatcher.UIThread.PushFrame(frame);
        }

        return result ?? (task.IsCompletedSuccessfully ? task.Result : false);
    }
}
