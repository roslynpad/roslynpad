using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This class is used to notify of an operation that affects a single <see cref="CompletionItem"/>.
    /// </summary>
    [DebuggerDisplay("EventArgs: {Item}")]
    public sealed class CompletionItemEventArgs : EventArgs
    {
        /// <summary>
        /// Relevant item
        /// </summary>
        public CompletionItem Item { get; }

        /// <summary>
        /// Constructs instance of <see cref="CompletionItemEventArgs"/>.
        /// </summary>
        public CompletionItemEventArgs(CompletionItem item)
        {
            this.Item = item ?? throw new ArgumentNullException(nameof(item));
        }
    }
}
