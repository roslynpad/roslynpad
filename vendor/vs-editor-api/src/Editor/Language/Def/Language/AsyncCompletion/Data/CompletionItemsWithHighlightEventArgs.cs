using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This class is used to notify of an operation that affects multiple <see cref="CompletionItemWithHighlight"/>s.
    /// </summary>
    [DebuggerDisplay("EventArgs: {Items.Length} items")]
    public sealed class ComputedCompletionItemsEventArgs : EventArgs
    {
        /// <summary>
        /// Relevant items
        /// </summary>
        public ComputedCompletionItems Items { get; }

        /// <summary>
        /// Constructs instance of <see cref="CompletionItemEventArgs"/>.
        /// </summary>
        public ComputedCompletionItemsEventArgs(ComputedCompletionItems items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }
    }
}
