namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Controls invocation and dismissal of Quick Info tooltips for <see cref="ITextView"/> instances.
    /// </summary>
    /// <remarks>
    /// This type can be called from any thread and will marshal its work to the UI thread as required.
    /// </remarks>
    public interface IAsyncQuickInfoBroker
    {
        /// <summary>
        /// Determines whether there is at least one active Quick Info session in the specified <see cref="ITextView" />.
        /// </summary>
        /// <remarks>
        /// Quick info is considered to be active if there is a visible, calculating, or recalculating quick info session.
        /// </remarks>
        /// <param name="textView">The <see cref="ITextView" /> for which Quick Info session status is to be determined.</</param>
        /// <returns>
        /// <c>true</c> if there is at least one active or calculating Quick Info session over the specified <see cref="ITextView" />, <c>false</c>
        /// otherwise.
        /// </returns>
        bool IsQuickInfoActive(ITextView textView);

        /// <summary>
        /// Triggers Quick Info tooltip in the specified <see cref="ITextView"/> at the caret or optional <paramref name="triggerPoint"/>.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        /// <paramref name="cancellationToken"/> was canceled by the caller or the operation was interrupted by another call to
        /// <see cref="TriggerQuickInfoAsync(ITextView, ITrackingPoint, QuickInfoSessionOptions, CancellationToken)"/>
        /// </exception>
        /// <param name="cancellationToken">If canceled before the method returns, cancels any computations in progress.</param>
        /// <param name="textView">
        /// The <see cref="ITextView" /> for which Quick Info is to be triggered.
        /// </param>
        /// <param name="triggerPoint">
        /// The <see cref="ITrackingPoint" /> in the view's text buffer at which Quick Info should be triggered.
        /// </param>
        /// <param name="options">Options for customizing Quick Info behavior.</param>
        /// <returns>
        /// An <see cref="IAsyncQuickInfoSession"/> tracking the state of the session or null if there are no items.
        /// </returns>
        Task<IAsyncQuickInfoSession> TriggerQuickInfoAsync(
            ITextView textView,
            ITrackingPoint triggerPoint = null,
            QuickInfoSessionOptions options = QuickInfoSessionOptions.None,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets Quick Info items for the <see cref="ITextView"/> at the <paramref name="triggerPoint"/>.
        /// </summary>
        /// <exception cref="OperationCanceledException">
        /// <paramref name="cancellationToken"/> was canceled by the caller.
        /// </exception>
        /// <exception cref="AggregateException">
        /// One or more errors occured during query of quick info items sources.
        /// </exception>
        /// <param name="cancellationToken">If canceled before the method returns, cancels any computations in progress.</param>
        /// <param name="textView">
        /// The <see cref="ITextView" /> for which Quick Info is to be triggered.
        /// </param>
        /// <param name="triggerPoint">
        /// The <see cref="ITrackingPoint" /> in the view's text buffer at which Quick Info should be triggered.
        /// </param>
        /// <param name="options">Options for customizing Quick Info behavior.</param>
        /// <returns>
        /// A series of Quick Info items and a span for which they are applicable.
        /// </returns>
        Task<QuickInfoItemsCollection> GetQuickInfoItemsAsync(
            ITextView textView,
            ITrackingPoint triggerPoint,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the current <see cref="IAsyncQuickInfoSession"/> for the <see cref="ITextView"/>.
        /// </summary>
        /// <remarks>
        /// Quick info is considered to be active if there is a visible, calculating, or recalculating quick info session.
        /// </remarks>
        /// <param name="textView">The <see cref="ITextView" /> for which to lookup the session.</param>
        /// <returns>The session, or <c>null</c> if there is no active session.</returns>
        IAsyncQuickInfoSession GetSession(ITextView textView);
    }
}
