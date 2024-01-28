namespace RoslynPad.UI;

public interface IAppDispatcher
{
    void InvokeAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal, CancellationToken cancellationToken = default);

    Task InvokeTaskAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal, CancellationToken cancellationToken = default);
}

public enum AppDispatcherPriority
{
    Normal,
    High,
    Low
}