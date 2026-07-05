namespace RoslynPad.UI;

public interface IAppDispatcher
{
    void InvokeAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal, CancellationToken cancellationToken = default);

    Task InvokeTaskAsync(Action action, AppDispatcherPriority priority = AppDispatcherPriority.Normal, CancellationToken cancellationToken = default);

    event Action<Exception>? UnhandledException;
}

public enum AppDispatcherPriority
{
    Normal,
    High,
    Low
}