using System.Threading;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A controller that enables and controls auto-cancellation of an operation execution by
    /// <see cref="IUIThreadOperationExecutor"/> on a timeout.
    /// </summary>
    public interface IUIThreadOperationTimeoutController
    {
        /// <summary>
        /// The duration (in milliseconds) after which an operation shouold be auto-cancelled.
        /// </summary>
        /// <remarks><see cref="Timeout.Infinite"/> disables auto-cancellation.</remarks>
        int CancelAfter { get; }

        /// <summary>
        /// Gets whether an operation, whose execution time exceeded <see cref="CancelAfter"/> timeout should be
        /// cancelled.
        /// </summary>
        /// <remarks>This callback can be used to disable auto-cancellation when an operation already
        /// passed the point of no cancellation and it would leave system in an inconsistent state.
        /// This method is called on a background thread.</remarks>
        bool ShouldCancel();

        /// <summary>
        /// An event callback raised when an operation execution timeout was reached.
        /// </summary>
        /// <param name="wasExecutionCancelled">Indicates whether an operation was auto-cancelled.
        /// Might be <c>false</c> if the operation is not cancellable (<see cref="IUIThreadOperationContext.AllowCancellation"/>
        /// is <c>false</c> or <see cref="ShouldCancel"/> returned <c>false</c>.
        /// </param>7
        /// <remarks>This method is called on a background thread.</remarks>
        void OnTimeout(bool wasExecutionCancelled);

        /// <summary>
        /// An event callback raised when a UI thread operation execution took long enough to be considered
        /// as a delay. Visual Studio implementation of the <see cref="IUIThreadOperationExecutor"/> displays
        /// a wait dialog at this point.
        /// </summary>
        /// <remarks>This method is called on a background thread.</remarks>
        void OnDelay();
    }
}
