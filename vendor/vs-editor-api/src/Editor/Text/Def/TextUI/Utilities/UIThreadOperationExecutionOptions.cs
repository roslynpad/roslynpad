using System;
using System.Threading;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Options that control behavior of <see cref="IUIThreadOperationExecutor"/>.
    /// </summary>
    public class UIThreadOperationExecutionOptions
    {
        /// <summary>
        /// Operation's title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Default operation's description, which is displayed on the wait dialog unless
        /// one or more <see cref="IUIThreadOperationScope"/>s with more specific descriptions were added to
        /// the <see cref="IUIThreadOperationContext"/>.
        /// </summary>
        public string DefaultDescription { get; }

        /// <summary>
        /// Whether to allow cancellability.
        /// </summary>
        public bool AllowCancellation { get; }

        /// <summary>
        /// Whether to show progress indication.
        /// </summary>
        public bool ShowProgress { get; }

        /// <summary>
        /// A controller that enables and controls auto-cancellation of an operation execution on a timeout.
        /// </summary>
        public IUIThreadOperationTimeoutController TimeoutController { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="UIThreadOperationExecutionOptions"/>.
        /// </summary>
        /// <param name="title">Operation's title. Can be null to indicate that the wait dialog should use the application's title.</param>
        /// <param name="defaultDescription">Default operation's description, which is displayed on the wait dialog unless
        /// one or more <see cref="IUIThreadOperationScope"/>s with more specific descriptions were added to
        /// the <see cref="IUIThreadOperationContext"/>.</param>
        /// <param name="allowCancellation">Whether to allow cancellability.</param>
        /// <param name="showProgress">Whether to show progress indication.</param>
        /// <param name="timeoutController">A controller that enables and controls auto-cancellation of an operation execution on a timeout.</param>
        public UIThreadOperationExecutionOptions(string title, string defaultDescription, bool allowCancellation, bool showProgress, IUIThreadOperationTimeoutController timeoutController = null)
        {
            Title = title;
            DefaultDescription = defaultDescription ?? throw new ArgumentNullException(nameof(defaultDescription));
            AllowCancellation = allowCancellation;
            ShowProgress = showProgress;

            // Timeout.Infinite is -1, other than that any negative value is invalid
            if (timeoutController?.CancelAfter < Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutController));
            }

            TimeoutController = timeoutController;
        }
    }
}
