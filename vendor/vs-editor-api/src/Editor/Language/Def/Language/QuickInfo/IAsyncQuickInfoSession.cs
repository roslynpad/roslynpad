namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Tracks state of a visible or calculating Quick Info session.
    /// </summary>
    public interface IAsyncQuickInfoSession : IPropertyOwner
    {
        /// <summary>
        /// Dispatched on the UI thread whenever the Quick Info Session changes state.
        /// </summary>
        event EventHandler<QuickInfoSessionStateChangedEventArgs> StateChanged;

        /// <summary>
        /// The span of text to which this Quick Info session applies.
        /// </summary>
        ITrackingSpan ApplicableToSpan { get; }

        /// <summary>
        /// The ordered, merged collection of content to be displayed in the Quick Info.
        /// </summary>
        /// <remarks>
        /// This field is originally null and is updated with the content once the session has
        /// finished querying the providers.
        /// </remarks>
        IEnumerable<object> Content { get; }

        /// <summary>
        /// Indicates that this Quick Info has interactive content that can request to stay open.
        /// </summary>
        bool HasInteractiveContent { get; }

        /// <summary>
        /// Specifies attributes of the Quick Info session and Quick Info session presentation.
        /// </summary>
        QuickInfoSessionOptions Options { get; }

        /// <summary>
        /// The current state of the Quick Info session.
        /// </summary>
        QuickInfoSessionState State { get; }

        /// <summary>
        /// The <see cref="ITextView"/> for which this Quick Info session was created.
        /// </summary>
        ITextView TextView { get; }

        /// <summary>
        /// Gets the point at which the Quick Info tip was triggered in the <see cref="ITextView"/>.
        /// </summary>
        /// <remarks>
        /// Returned <see cref="ITrackingPoint"/> is on the buffer requested by the caller.
        /// </remarks>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> relative to which to obtain the point.</param>
        /// <returns>A <see cref="ITrackingPoint"/> indicating the point over which Quick Info was invoked.</returns>
        ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer);

        /// <summary>
        /// Gets the point at which the Quick Info tip was triggered in the <see cref="ITextView"/>.
        /// </summary>
        /// <remarks>
        /// Returned point is on the buffer requested by the caller.
        /// </remarks>
        /// <param name="snapshot">The <see cref="ITextSnapshot"/> relative to which to obtain the point.</param>
        /// <returns>The point over which Quick Info was invoked or <c>null</c> if it does not exist in <paramref name="snapshot"/>.</returns>
        SnapshotPoint? GetTriggerPoint(ITextSnapshot snapshot);

        /// <summary>
        /// Dismisses the Quick Info session, if applicable. If the session is already dismissed,
        /// this method no-ops.
        /// </summary>
        /// <returns>A task tracking the completion of the operation.</returns>
        Task DismissAsync();
    }
}
