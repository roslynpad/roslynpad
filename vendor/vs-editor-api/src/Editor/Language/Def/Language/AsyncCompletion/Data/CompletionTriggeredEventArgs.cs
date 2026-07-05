using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This class is used to notify about new <see cref="IAsyncCompletionSession"/> being triggered
    /// </summary>
    public sealed class CompletionTriggeredEventArgs : EventArgs
    {
        /// <summary>
        /// Newly created <see cref="IAsyncCompletionSession"/>.
        /// </summary>
        public IAsyncCompletionSession CompletionSession { get; }

        /// <summary>
        /// <see cref="ITextView"/> where completion was triggered.
        /// </summary>
        public ITextView TextView { get; }

        /// <summary>
        /// Constructs instance of <see cref="CompletionItemSelectedEventArgs"/>.
        /// </summary>
        /// <param name="completionSession">Newly created <see cref="IAsyncCompletionSession"/></param>
        /// <param name="textView"><see cref="ITextView"/> where completion was triggered</param>
        public CompletionTriggeredEventArgs(IAsyncCompletionSession completionSession, ITextView textView)
        {
            this.CompletionSession = completionSession;
            this.TextView = textView;
        }
    }
}
