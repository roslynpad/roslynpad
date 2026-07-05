using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// This class is used to notify completion's logic of selecting through the UI
    /// </summary>
    [DebuggerDisplay("EventArgs: {SelectedItem}, is suggestion: {SuggestionItemSelected}")]
    public sealed class CompletionItemSelectedEventArgs : EventArgs
    {
        /// <summary>
        /// Selected item. Might be null if there is no selection
        /// </summary>
        public CompletionItem SelectedItem { get; }

        /// <summary>
        /// Whether selected item is a suggestion mode item
        /// </summary>
        public bool SuggestionItemSelected { get; }

        /// <summary>
        /// Constructs instance of <see cref="CompletionItemSelectedEventArgs"/>.
        /// </summary>
        /// <param name="selectedItem">User-selected item</param>
        /// <param name="suggestionItemSelected">Whether the selected item is a suggestion item</param>
        public CompletionItemSelectedEventArgs(CompletionItem selectedItem, bool suggestionItemSelected)
        {
            this.SelectedItem = selectedItem;
            this.SuggestionItemSelected = suggestionItemSelected;
        }
    }
}
