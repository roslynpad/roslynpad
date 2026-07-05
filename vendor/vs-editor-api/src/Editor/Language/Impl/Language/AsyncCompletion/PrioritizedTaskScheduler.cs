using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Clone of Roslyn's PrioritizedTaskScheduler
    /// </summary>
    internal class PrioritizedTaskScheduler : TaskScheduler
    {
        public static readonly TaskScheduler AboveNormalInstance = new PrioritizedTaskScheduler(ThreadPriority.AboveNormal);

        private readonly Thread _thread;
        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

        private PrioritizedTaskScheduler(ThreadPriority priority)
        {
            _thread = new Thread(ThreadStart)
            {
                Priority = priority,
                IsBackground = true,
                Name = this.GetType().Name + "-" + priority,
            };

            _thread.Start();
        }

        private void ThreadStart()
        {
            while (true)
            {
                var task = _tasks.Take();
                bool ret = this.TryExecuteTask(task);
                Debug.Assert(ret);
            }
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread != _thread)
            {
                // Don't allow tasks to execute on other threads.
                return false;
            }
            return this.TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // NOTE(cyrusn): This method is only for debugging purposes.
            return _tasks.ToArray();
        }
    }
}
