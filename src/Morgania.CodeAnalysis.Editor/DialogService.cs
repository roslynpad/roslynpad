using System.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Shows dialogs through the host-exported <see cref="IDialogPresenter"/> (falling back to
/// modal windows), bridging the presenter's async result to the synchronous calls Roslyn's
/// options services make by pushing a dispatcher frame.
/// </summary>
[Export, Shared]
[method: ImportingConstructor]
internal sealed class DialogService([ImportMany] IEnumerable<IDialogPresenter> presenters)
{
    private readonly IDialogPresenter _presenter = presenters.FirstOrDefault() ?? new WindowDialogPresenter();

    public bool? ShowDialog(DialogView dialog)
    {
        var task = _presenter.ShowDialogAsync(dialog);

        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.PushFrame(frame);
        }

        return task.IsCompletedSuccessfully ? task.Result : false;
    }

    private sealed class WindowDialogPresenter : IDialogPresenter
    {
        public async Task<bool?> ShowDialogAsync(IDialogView dialog)
        {
            var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?
                .Windows.FirstOrDefault(w => w.IsActive);
            if (owner == null) return false;

            var window = new Window
            {
                Content = dialog,
                Title = dialog.Title,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            dialog.CloseRequested += (_, result) => window.Close(result);

            return await window.ShowDialog<bool?>(owner).ConfigureAwait(true);
        }
    }
}
