using System;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Build
{
    public static class SemaphoreSlimExtensions
    {
        public static SemaphoreDisposer DisposableWait(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            semaphore.Wait(cancellationToken);
            return new SemaphoreDisposer(semaphore);
        }

        public static async ValueTask<SemaphoreDisposer> DisposableWaitAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new SemaphoreDisposer(semaphore);
        }

        public struct SemaphoreDisposer : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public SemaphoreDisposer(SemaphoreSlim semaphore) => _semaphore = semaphore;

            public void Dispose() => _semaphore.Release();
        }
    }
}
