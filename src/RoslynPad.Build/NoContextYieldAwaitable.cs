using System.Runtime.CompilerServices;

namespace RoslynPad.Build;

/// <summary>
/// Yields to the thread-pool.
/// </summary>
public readonly struct NoContextYieldAwaitable
{
    public NoContextYieldAwaiter GetAwaiter() => new();

    public readonly struct NoContextYieldAwaiter : ICriticalNotifyCompletion
    {
        public bool IsCompleted => Thread.CurrentThread.IsThreadPoolThread;

        public void OnCompleted(Action continuation) => QueueContinuation(continuation, flowContext: false);

        public void UnsafeOnCompleted(Action continuation) => QueueContinuation(continuation, flowContext: false);

        private static void QueueContinuation(Action continuation, bool flowContext)
        {
            ArgumentNullException.ThrowIfNull(continuation);

            if (flowContext)
            {
                ThreadPool.QueueUserWorkItem(s_waitCallbackRunAction, continuation);
            }
            else
            {
                ThreadPool.UnsafeQueueUserWorkItem(s_waitCallbackRunAction, continuation);
            }
        }

        private static readonly WaitCallback s_waitCallbackRunAction = RunAction!;

        private static void RunAction(object state) => ((Action)state)();

        public void GetResult() { }
    }
}
