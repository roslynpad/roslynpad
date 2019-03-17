using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Runtime
{
    public static class Helpers
    {
        private static readonly Lazy<Task<SynchronizationContext>> _dispatcherTask = new Lazy<Task<SynchronizationContext>>(CreateWpfDispatcherAsync);

        public static async Task<SynchronizationContext> CreateWpfDispatcherAsync()
        {
            // TODO: Make this work with WPF on Core
            var windowsBaseAssembly = Assembly.Load("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            var dispatcherType = windowsBaseAssembly.GetType("System.Windows.Threading.Dispatcher", throwOnError: true);
            var dispatcherSyncContextCtor = windowsBaseAssembly.GetType("System.Windows.Threading.DispatcherSynchronizationContext", throwOnError: true)
                .GetConstructors().FirstOrDefault(c => c.GetParameters() is var p && p.Length == 1 && p[0].ParameterType.Name == "Dispatcher");
            var runMethod = (Action)dispatcherType.GetMethod("Run", Array.Empty<Type>()).CreateDelegate(typeof(Action));
            var currentDispatcherProperty = dispatcherType.GetProperty("CurrentDispatcher");

            var tcs = new TaskCompletionSource<object>();

            var thread = new Thread(() =>
            {
                var dispatcher = currentDispatcherProperty.GetValue(null);
                tcs.SetResult(dispatcher);
                runMethod();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            var dispatcher = await tcs.Task.ConfigureAwait(false);

            return (SynchronizationContext)dispatcherSyncContextCtor.Invoke(new[] { dispatcher });
        }

        public static SynchronizationContextAwaitable RunWpfAsync()
        {
            return new SynchronizationContextAwaitable(_dispatcherTask.Value);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public struct SynchronizationContextAwaitable
        {
            private readonly Task<SynchronizationContext> _task;

            public SynchronizationContextAwaitable(Task<SynchronizationContext> task)
            {
                _task = task;
            }

            public SynchronizationContextAwaiter GetAwaiter() => new SynchronizationContextAwaiter(_task);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public struct SynchronizationContextAwaiter : INotifyCompletion
        {
            private static readonly SendOrPostCallback _postCallback = state => ((Action)state)();

            private readonly Task<SynchronizationContext> _task;

            public SynchronizationContextAwaiter(Task<SynchronizationContext> task)
            {
                _task = task;
            }

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                if (_task.Status == TaskStatus.RanToCompletion)
                {
                    _task.Result.Post(_postCallback, continuation);
                }
                else
                {
                    _task.ContinueWith(t => t.Result.Post(_postCallback, continuation));
                }
            }

            public void GetResult() { }
        }
    }
}
