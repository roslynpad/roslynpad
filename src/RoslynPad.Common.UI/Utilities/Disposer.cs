namespace RoslynPad.UI.Utilities;

internal sealed class Disposer(Action onDispose) : IDisposable
{
    private readonly Action _onDispose = onDispose;

    public void Dispose() => _onDispose?.Invoke();
}