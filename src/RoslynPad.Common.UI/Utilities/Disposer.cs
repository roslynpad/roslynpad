using System;

namespace RoslynPad.UI.Utilities
{
    internal sealed class Disposer : IDisposable
    {
        private readonly Action _onDispose;

        public Disposer(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose() => _onDispose?.Invoke();
    }
}