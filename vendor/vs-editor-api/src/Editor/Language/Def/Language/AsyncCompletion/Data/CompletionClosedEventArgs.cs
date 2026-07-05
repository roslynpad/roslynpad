using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This class is used to notify completion's logic when the UI closes
    /// </summary>
    public sealed class CompletionClosedEventArgs : EventArgs
    {
        /// <summary>
        /// <see cref="ITextView"/> that hosted completion UI
        /// </summary>
        public ITextView TextView { get; }

        /// <summary>
        /// Constructs instance of <see cref="CompletionClosedEventArgs"/>.
        /// </summary>
        /// <param name="textView"><see cref="ITextView"/> that hosted this completion UI</param>
        public CompletionClosedEventArgs(ITextView textView)
        {
            TextView = textView;
        }
    }
}
