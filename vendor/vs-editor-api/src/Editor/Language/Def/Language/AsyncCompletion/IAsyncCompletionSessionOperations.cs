using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Exposes non-public functionality to commanding and tests
    /// </summary>
    public interface IAsyncCompletionSessionOperations : IAsyncCompletionSession
    {
        /// <summary>
        /// Sets span applicable to this completion session.
        /// The span is defined on the session's <see cref="ITextView.TextBuffer"/>.
        /// </summary>
        new ITrackingSpan ApplicableToSpan { get; set; }

        /// <summary>
        /// Returns whether computation has begun.
        /// Computation starts after calling <see cref="IAsyncCompletionSession.OpenOrUpdate(CompletionTrigger, SnapshotPoint, CancellationToken)"/>
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Returns the intial trigger 
        /// </summary>
        CompletionTrigger InitialTrigger { get; }

        /// <summary>
        /// Returns the location of the initial trigger
        /// </summary>
        SnapshotPoint InitialTriggerLocation { get; }

        /// <summary>
        /// Enqueues selection a specified item. When all queued tasks are completed, the UI updates.
        /// </summary>
        void SelectCompletionItem(CompletionItem item);

        /// <summary>
        /// Enqueues setting suggestion mode. When all queued tasks are completed, the UI updates.
        /// </summary>
        void SetSuggestionMode(bool useSuggestionMode);

        /// <summary>
        /// Commits unique item. If no items were computed, performs computation. If there is no unique item, shows the UI.
        /// </summary>
        void InvokeAndCommitIfUnique(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token);

        /// <summary>
        /// Starts asynchronous computation, which results in either committing of the single <see cref="CompletionItem"/> or opening the completion UI.
        /// Calling <see cref="OpenOrUpdate(CompletionTrigger, SnapshotPoint, CancellationToken)"/> cancels the operation and dismisses the session. 
        /// Must be called on the UI thread to correctly set state of the session.
        /// </summary>
        /// <param name="token">Token used to cancel this operation</param>
        /// <returns><c>true</c> if the unique item was committed</returns>
        Task<bool> CommitIfUniqueAsync(CancellationToken token);

        /// <summary>
        /// Enqueues selecting the next item. When all queued tasks are completed, the UI updates.
        /// </summary>
        void SelectDown();

        /// <summary>
        /// Enqueues selecting the item on the next page. When all queued tasks are completed, the UI updates.
        /// </summary>
        void SelectPageDown();

        /// <summary>
        /// Enqueues selecting the previous item. When all queued tasks are completed, the UI updates.
        /// </summary>
        void SelectUp();

        /// <summary>
        /// Enqueues selecting the item on the previous page. When all queued tasks are completed, the UI updates.
        /// </summary>
        void SelectPageUp();
    }

    public interface IAsyncCompletionSessionOperations2 : IAsyncCompletionSessionOperations
    {
        bool CanToggleFilter(string accessKey);
        void ToggleFilter(string accessKey);
    }
}
