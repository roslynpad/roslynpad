using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Represents an object that provides <see cref="CompletionItem"/>s and other information
    /// relevant to the completion feature at a specific <see cref="SnapshotPoint"/>.
    /// Additionally, this object has capability to provide additional <see cref="CompletionItem"/>s
    /// in a reaction to user interacting with <see cref="CompletionExpander"/>. If this capability
    /// is not necessary, then it is sufficient to implement just <see cref="IAsyncCompletionSource"/>.
    /// </summary>
    /// <remarks>
    /// Instances of this class should be created by <see cref="IAsyncCompletionSourceProvider"/>, which is a MEF part.
    /// </remarks>
    public interface IAsyncExpandingCompletionSource : IAsyncCompletionSource
    {
        /// <summary>
        /// Called when user interacts with expander buttons,
        /// requesting the completion source to provide additional completion items pertinent to the expander button.
        /// For best performance, do not provide <see cref="CompletionContext.Filters"/> unless expansion should add new filters.
        /// Called on a background thread.
        /// </summary>
        /// <param name="session">Reference to the active <see cref="IAsyncCompletionSession"/></param>
        /// <param name="expander">Expander which caused this call</param>
        /// <param name="initialTrigger">What initially caused the completion</param>
        /// <param name="applicableToSpan">Location where completion will take place, on the view's data buffer: <see cref="ITextView.TextBuffer"/></param>
        /// <param name="token">Cancellation token that may interrupt this operation</param>
        /// <returns>A struct that holds completion items and applicable span</returns>
        Task<CompletionContext> GetExpandedCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionExpander expander,
            CompletionTrigger initialTrigger,
            SnapshotSpan applicableToSpan,
            CancellationToken token);
    }
}
