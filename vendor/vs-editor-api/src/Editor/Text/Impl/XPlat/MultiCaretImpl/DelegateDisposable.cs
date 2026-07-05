using System;

namespace Microsoft.VisualStudio.Text.MultiSelection.Implementation
{
    internal class DelegateDisposable : IDisposable
    {
        private Action _onDisposed;

        public DelegateDisposable(Action onDisposed)
        {
            _onDisposed = onDisposed;
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            _onDisposed();
        }
    }
}
