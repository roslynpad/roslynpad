using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation.AsyncCompletion
{
    internal class DeferredBlockingOperation<T>
    {
        public JoinableTask<T> Operation { get; }

        private CancellationTokenSource CancellationSource { get; }
        private bool _canceled = false;

        /// <summary>
        /// Create instance of <see cref="DeferredBlockingOperation"/>, which wraps a blocking <paramref name="operation"/>
        /// that is immediately run on the background thread, can be canceled via <paramref name="token"/>
        /// and accessed via <see cref="Operation"/>
        /// </summary>
        /// <param name="jtc">Reference to <see cref="JoinableTaskContext"/></param>
        /// <param name="operation">Blocking operation</param>
        /// <param name="token">Token used to cancel the blocking operation</param>
        public DeferredBlockingOperation(JoinableTaskContext jtc, Func<CancellationToken, Task<T>> operation, CancellationToken token)
        {
            CancellationSource = new CancellationTokenSource();
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationSource.Token, token);
            Operation = jtc.Factory.RunAsync<T>(async () =>
            {
                await TaskScheduler.Default; // switch to background thread
                return await operation(linkedSource.Token).ConfigureAwait(false); // run the blocking operation
            });
        }

        internal void Cancel()
        {
            if (_canceled)
                return;

            CancellationSource.Cancel();
            CancellationSource.Dispose();
            _canceled = true;
        }
    }
}
