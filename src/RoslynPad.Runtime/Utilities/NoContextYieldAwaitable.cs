using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RoslynPad.Utilities
{
    /// <summary>
    /// Yields to the thread-pool.
    /// </summary>
    public readonly struct NoContextYieldAwaitable
    {
        public NoContextYieldAwaiter GetAwaiter() { return new NoContextYieldAwaiter(); }

        public readonly struct NoContextYieldAwaiter : ICriticalNotifyCompletion
        {
            public bool IsCompleted => Thread.CurrentThread.IsThreadPoolThread;

            public void OnCompleted(Action continuation) => QueueContinuation(continuation, flowContext: false);

            public void UnsafeOnCompleted(Action continuation) => QueueContinuation(continuation, flowContext: false);

            private static void QueueContinuation(Action continuation, bool flowContext)
            {
                if (continuation == null) throw new ArgumentNullException(nameof(continuation));

                if (flowContext)
                {
                    ThreadPool.QueueUserWorkItem(s_waitCallbackRunAction, continuation);
                }
                else
                {
                    ThreadPool.UnsafeQueueUserWorkItem(s_waitCallbackRunAction, continuation);
                }
            }

            private static readonly WaitCallback s_waitCallbackRunAction = RunAction;

            private static void RunAction(object state) => ((Action)state)();

            public void GetResult() { }
        }
    }
}
