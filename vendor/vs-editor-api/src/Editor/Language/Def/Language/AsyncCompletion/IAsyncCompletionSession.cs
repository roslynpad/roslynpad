using System;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Represents a class that tracks completion within a single <see cref="ITextView"/>.
    /// Constructed and managed by an instance of <see cref="IAsyncCompletionBroker"/>
    /// </summary>
    public interface IAsyncCompletionSession : IPropertyOwner
    {
        /// <summary>
        /// Request completion to be opened or updated in a given location,
        /// the completion items to be filtered and sorted, and the UI updated.
        /// Must be called on UI thread. Enqueues work on a worker thread.
        /// </summary>
        /// <param name="trigger">What caused completion</param>
        /// <param name="triggerLocation">Location of the trigger on the subject buffer</param>
        /// <param name="token">Token used to cancel this and other queued operation.</param>
        void OpenOrUpdate(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token);

        /// <summary>
        /// Stops the session and hides associated UI.
        /// May be called from any thread.
        /// </summary>
        void Dismiss();

        /// <summary>
        /// Returns whether given text edit should result in committing this session.
        /// Since this method is on a typing hot path, it returns quickly if the <paramref name="typedChar"/>
        /// is not found among characters collected from <see cref="IAsyncCompletionCommitManager.PotentialCommitCharacters"/>
        /// Else, we map the top-buffer <paramref name="triggerLocation"/> to subject buffers and query
        /// <see cref="IAsyncCompletionCommitManager.ShouldCommitCompletion(IAsyncCompletionSession, SnapshotPoint, char, CancellationToken)"/>
        /// to see whether any <see cref="IAsyncCompletionCommitManager"/> would like to commit completion.
        /// Must be called on UI thread.
        /// </summary>
        /// <remarks>This method must run on UI thread because of mapping the point across buffers.</remarks>
        /// <param name="typedChar">The text edit which caused this action. May be null.</param>
        /// <param name="triggerLocation">Location on the view's data buffer: <see cref="ITextView.TextBuffer"/></param>
        /// <param name="token">Token used to cancel this operation</param>
        /// <returns>Whether any <see cref="IAsyncCompletionCommitManager.ShouldCommitCompletion(IAsyncCompletionSession, SnapshotPoint, char, CancellationToken)"/> returned true</returns>
        bool ShouldCommit(char typedChar, SnapshotPoint triggerLocation, CancellationToken token);

        /// <summary>
        /// Commits the currently selected <see cref="CompletionItem"/>.
        /// Must be called on UI thread.
        /// </summary>
        /// <param name="typedChar">The text edit which caused this action.
        /// May be default(char) when commit was requested by an explcit command (e.g. hitting Tab, Enter or clicking)</param>
        /// <param name="token">Token used to cancel this operation</param>
        /// <returns>Instruction for the editor how to proceed after invoking this method</returns>
        CommitBehavior Commit(char typedChar, CancellationToken token);

        /// <summary>
        /// Commits the single <see cref="CompletionItem"/> or opens the completion UI.
        /// Must be called on UI thread.
        /// </summary>
        /// <param name="token">Token used to cancel this operation</param>
        /// <returns><c>true</c> if the unique item was committed</returns>
        bool CommitIfUnique(CancellationToken token);

        /// <summary>
        /// Returns the <see cref="ITextView"/> this session is active on.
        /// </summary>
        ITextView TextView { get; }

        /// <summary>
        /// Gets span applicable to this completion session.
        /// The span is defined on the session's <see cref="ITextView.TextBuffer"/>.
        /// </summary>
        ITrackingSpan ApplicableToSpan { get; }

        /// <summary>
        /// Returns whether session is dismissed.
        /// When session is dismissed, all work is canceled.
        /// </summary>
        bool IsDismissed { get; }

        /// <summary>
        /// Raised on UI thread when completion item is committed
        /// </summary>
        event EventHandler<CompletionItemEventArgs> ItemCommitted;

        /// <summary>
        /// Raised on UI thread when completion session is dismissed.
        /// </summary>
        event EventHandler Dismissed;

        /// <summary>
        /// Provides elements that are visible in the UI
        /// Raised on worker thread when filtering and sorting of items has finished.
        /// There may be more updates happening immediately after this update.
        /// </summary>
        event EventHandler<ComputedCompletionItemsEventArgs> ItemsUpdated;

        /// <summary>
        /// Gets items visible in the UI and information about selection.
        /// This is a blocking call. As a side effect, prevents the UI from displaying.
        /// </summary>
        ComputedCompletionItems GetComputedItems(CancellationToken token);
    }
}
