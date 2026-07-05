using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Represents a class that filters and sorts available <see cref="CompletionItem"/>s given the current state of the editor.
    /// It also declares which completion filters are available for the returned subset of <see cref="CompletionItem"/>s.
    /// All methods are called on background thread.
    /// </summary>
    /// <remarks>
    /// Instances of this class should be created by <see cref="IAsyncCompletionItemManagerProvider"/>, which is a MEF part.
    /// </remarks>
    public interface IAsyncCompletionItemManager
    {
        /// <summary>
        /// This method is called before completion is about to appear,
        /// on subsequent typing events and when user toggles completion filters.
        /// <paramref name="session"/> tracks user user's input tracked with <see cref="IAsyncCompletionSession.ApplicableToSpan"/>.
        /// <paramref name="data"/> provides applicable <see cref="AsyncCompletionSessionDataSnapshot.Snapshot"/> and 
        /// and <see cref="AsyncCompletionSessionDataSnapshot.SelectedFilters"/>s that indicate user's filter selection.
        /// </summary>
        /// <param name="session">The active <see cref="IAsyncCompletionSession"/>. See <see cref="IAsyncCompletionSession.ApplicableToSpan"/> and <see cref="IAsyncCompletionSession.TextView"/></param>
        /// <param name="data">Contains properties applicable at the time this method is invoked.</param>
        /// <param name="token">Cancellation token that may interrupt this operation</param>
        /// <returns>Instance of <see cref="FilteredCompletionModel"/> that contains completion items to render, filters to display and recommended item to select</returns>
        Task<FilteredCompletionModel> UpdateCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionDataSnapshot data,
            CancellationToken token);

        /// <summary>
        /// This method is first called before completion is about to appear.
        /// The result of this method will be used in subsequent invocations of <see cref="UpdateCompletionListAsync"/>
        /// <paramref name="session"/> tracks user user's input tracked with <see cref="IAsyncCompletionSession.ApplicableToSpan"/>.
        /// <paramref name="data"/> provides applicable <see cref="AsyncCompletionSessionDataSnapshot.Snapshot"/> and 
        /// </summary>
        /// <param name="session">The active <see cref="IAsyncCompletionSession"/>. See <see cref="IAsyncCompletionSession.TextView"/></param>
        /// <param name="data">Contains properties applicable at the time this method is invoked.</param>
        /// <param name="token">Cancellation token that may interrupt this operation</param>
        /// <returns>Sorted <see cref="ImmutableArray"/> of <see cref="CompletionItem"/> that will be subsequently passed to <see cref="UpdateCompletionListAsync"/></returns>
        Task<ImmutableArray<CompletionItem>> SortCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken token);
    }
}
