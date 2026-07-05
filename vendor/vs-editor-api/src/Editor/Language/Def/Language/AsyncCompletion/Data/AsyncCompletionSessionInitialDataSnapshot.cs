using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Contains data of <see cref="IAsyncCompletionSession"/> valid at a specific, instantaneous moment pertinent to current computation.
    /// This data is passed to <see cref="IAsyncCompletionItemManager"/> to initially sort the list prior to filtering and selecting.
    /// </summary>
    public class AsyncCompletionSessionInitialDataSnapshot
    {
        /// <summary>
        /// Set of <see cref="CompletionItem"/>s to sort.
        /// </summary>
        public ImmutableArray<CompletionItem> InitialList { get; }

        /// <summary>
        /// Set of <see cref="CompletionItem"/>s to sort, as a list. Equivalent to <see cref="InitialList"/>.
        /// </summary>
        public IReadOnlyList<CompletionItem> InitialItemList => InitialList;

        /// <summary>
        /// The <see cref="ITextSnapshot"/> applicable for this computation. The snapshot comes from the view's data buffer.
        /// </summary>
        public ITextSnapshot Snapshot { get; }

        /// <summary>
        /// The <see cref="CompletionTrigger"/> that started this completion session.
        /// </summary>
        public CompletionTrigger Trigger { get; }

        /// <summary>
        /// Constructs <see cref="AsyncCompletionSessionInitialDataSnapshot"/>
        /// </summary>
        /// <param name="initialList">Set of <see cref="CompletionItem"/>s to sort</param>
        /// <param name="snapshot">The <see cref="ITextSnapshot"/> applicable for this computation. The snapshot comes from the view's data buffer</param>
        /// <param name="trigger">The <see cref="CompletionTrigger"/> that started this completion session</param>
        public AsyncCompletionSessionInitialDataSnapshot(
            ImmutableArray<CompletionItem> initialList,
            ITextSnapshot snapshot,
            CompletionTrigger trigger
        )
        {
            InitialList = initialList;
            Snapshot = snapshot;
            Trigger = trigger;
        }
    }
}
