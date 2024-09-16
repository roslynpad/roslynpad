using RoslynPad.UI;
using Avalonia.Threading;

namespace RoslynPad;

[Export(typeof(IAppDispatcher))]
public class AppDispatcher : IAppDispatcher
{
    public void InvokeAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal,
        CancellationToken cancellationToken = new CancellationToken())
    {
        _ = InternalInvoke(action, priority, cancellationToken);
    }

    public Task InvokeTaskAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return InternalInvoke(action, priority, cancellationToken);
    }

    private Task InternalInvoke(Action action, AppDispatcherPriority priority, CancellationToken cancellationToken)
    {
        return Dispatcher.UIThread.InvokeAsync(action, ConvertPriority(priority), cancellationToken).GetTask();
    }

    private DispatcherPriority ConvertPriority(AppDispatcherPriority priority) => priority switch
    {
        AppDispatcherPriority.Normal => DispatcherPriority.Normal,
        AppDispatcherPriority.High => DispatcherPriority.Send,
        AppDispatcherPriority.Low => DispatcherPriority.Background,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
    };
}
