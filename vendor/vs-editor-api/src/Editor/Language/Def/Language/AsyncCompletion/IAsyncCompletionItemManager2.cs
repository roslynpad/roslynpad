using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Extends <see cref="IAsyncCompletionItemManager"/> with a sort method that returns a
    /// <see cref="CompletionList{T}"/> instead of an immutable array. When an item manager
    /// implements this interface, the session prefers <see cref="SortCompletionItemListAsync"/>
    /// over <see cref="IAsyncCompletionItemManager.SortCompletionListAsync"/>.
    /// </summary>
    public interface IAsyncCompletionItemManager2 : IAsyncCompletionItemManager
    {
        /// <summary>
        /// Sorts the initial list of completion items.
        /// </summary>
        Task<CompletionList<CompletionItem>> SortCompletionItemListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken token);
    }

    /// <summary>
    /// Extension methods for <see cref="IAsyncCompletionSession"/>.
    /// </summary>
    public static class AsyncCompletionSessionExtensions
    {
        /// <summary>
        /// Creates a <see cref="CompletionList{T}"/> for use with this session.
        /// </summary>
        public static CompletionList<T> CreateCompletionList<T>(this IAsyncCompletionSession session, IEnumerable<T> items)
            => new CompletionList<T>(items);
    }
}
