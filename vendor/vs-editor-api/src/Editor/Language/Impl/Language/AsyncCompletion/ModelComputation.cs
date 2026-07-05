using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Facilitates enqueuing tasks to be ran on a worker thread.
    /// Each task takes an immutable instance of <typeparamref name="TModel"/>
    /// and outputs an instance of <typeparamref name="TModel"/>.
    /// The returned instance will serve as input to the next task.
    /// </summary>
    /// <typeparam name="TModel">Type that represents a snapshot of feature's state</typeparam>
    sealed class ModelComputation<TModel> where TModel : class
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly TaskScheduler _computationTaskScheduler;
        private readonly CancellationToken _token;
        private readonly IGuardedOperations _guardedOperations;
        private readonly IModelComputationCallbackHandler<TModel> _callbacks;

        private bool _terminated;
        private JoinableTask<TModel> _lastJoinableTask;
        private CancellationTokenSource _uiCancellation;

        internal TModel RecentModel { get; private set; } = default;

        /// <summary>
        /// Creates an instance of <see cref="ModelComputation{TModel}"/>
        /// and enqueues an task that will generate the initial state of the <typeparamref name="TModel"/>
        /// </summary>
#pragma warning disable CA1068 // CancellationToken should be the last parameter
        public ModelComputation(
            TaskScheduler computationTaskScheduler,
            JoinableTaskContext joinableTaskContext,
            Func<TModel, CancellationToken, Task<TModel>> initialTransformation,
            CancellationToken token,
            IGuardedOperations guardedOperations,
            IModelComputationCallbackHandler<TModel> callbacks)
#pragma warning restore CA1068
        {
            _joinableTaskFactory = joinableTaskContext.Factory;
            _computationTaskScheduler = computationTaskScheduler;
            _token = token;
            _guardedOperations = guardedOperations;
            _callbacks = callbacks;

            // Start dummy tasks so that we don't need to check for null on first Enqueue
            _lastJoinableTask = _joinableTaskFactory.RunAsync(() => Task.FromResult(default(TModel)));
            _uiCancellation = new CancellationTokenSource();

            // Immediately run the first transformation, to operate on proper TModel.
            Enqueue(initialTransformation, updateUi: false);
        }

        /// <summary>
        /// Schedules work to be done on the background,
        /// potentially preempted by another piece of work scheduled in the future,
        /// <paramref name="updateUi" /> indicates whether a single piece of work should occue once all background work is completed.
        /// </summary>
        public void Enqueue(Func<TModel, CancellationToken, Task<TModel>> transformation, bool updateUi)
        {
            // The integrity of our sequential chain depends on this method not being called concurrently.
            // So we require the UI thread.
            if (!_joinableTaskFactory.Context.IsOnMainThread)
                throw new InvalidOperationException($"This method must be callled on the UI thread.");

            if (_token.IsCancellationRequested || _terminated)
                return; // Don't enqueue after computation has stopped.

            // Attempt to commit (CommitIfUnique) will cancel the UI updates. If the commit failed, we still want to update the UI.
            if (_uiCancellation.IsCancellationRequested)
                _uiCancellation = new CancellationTokenSource();

            var previousTask = _lastJoinableTask;
            JoinableTask<TModel> currentTask = null;
            currentTask = _joinableTaskFactory.RunAsync(async () =>
            {
                await Task.Yield(); // Yield to guarantee that currentTask is assigned.
                await _computationTaskScheduler; // Go to the above priority thread. Main thread will return as soon as possible.
                try
                {
                    var previousModel = await previousTask;

                    if (_token.IsCancellationRequested || _terminated)
                        return previousModel;

                    // Previous task finished processing. We are ready to execute next piece of work.
                    var transformedModel = await transformation(previousModel, _token).ConfigureAwait(true);
                    RecentModel = transformedModel;

                    // TODO: Consider updating UI even if updateUi is false but it wasn't updated yet.
                    if (_lastJoinableTask == currentTask && !_token.IsCancellationRequested && !_terminated)
                    {
                        _callbacks.ComputationFinished(transformedModel);

                        if (updateUi && !_uiCancellation.IsCancellationRequested)
                            _callbacks.UpdateUI(transformedModel, _uiCancellation.Token).Forget();
                    }
                    return transformedModel;
                }
                catch (Exception ex) when (ex is OperationCanceledException || ex is ThreadAbortException)
                {
                    // Disallow enqueuing more tasks
                    _terminated = true;
                    // Close completion
                    _callbacks.DismissDueToCancellation();
                    // Return a task that has not faulted
                    return default(TModel);
                }
                catch (Exception ex)
                {
                    // Disallow enqueuing more tasks
                    _terminated = true;
                    // Log the issue
                    _guardedOperations.HandleException(this, ex);
                    // Close completion
                    _callbacks.DismissDueToError();
                    // Return a task that has not faulted
                    return default(TModel);
                }
            });

            _lastJoinableTask = currentTask;
        }

        /// <summary>
        /// Blocks, waiting for all background work to finish.
        /// </summary>
        /// <param name="cancelUi">Whether UI should be dismissed. If false, this method will return after UI has been rendered</param>
        /// <param name="dontWaitForUpdatedModel">Returns last available model without block. Used in WYSIWYG mode.</param>
        /// <param name="token">Token used to cancel the operation, unblock the thread and return null</param>
        /// <returns></returns>
        public TModel WaitAndGetResult(bool cancelUi, CancellationToken token)
        {
            if (cancelUi)
                _uiCancellation.Cancel();

            try
            {
                return _lastJoinableTask.Join(token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
    }
}
