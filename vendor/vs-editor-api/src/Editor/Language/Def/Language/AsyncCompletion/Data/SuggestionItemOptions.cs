using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Instructs the editor if and how to display the suggestion item.
    /// When in suggestion mode, the UI displays a single <see cref="CompletionItem"/> whose <see cref="CompletionItem.DisplayText"/>
    /// and <see cref="CompletionItem.InsertText"/> is equal to text typed by the user so far.
    /// This class specifies the tooltip to use for this item, and <see cref="CompletionItem.DisplayText"/> when user has not typed anything.
    /// </summary>
    public sealed class SuggestionItemOptions
    {
        /// <summary>
        /// Text to use as suggestion item's <see cref="CompletionItem.DisplayText"/> when user has not typed anything.
        /// Usually prompts user to begin typing and describes what does the suggestion item represent.
        /// </summary>
        public string DisplayTextWhenEmpty { get; }

        /// <summary>
        /// Localized tooltip text for the suggestion item.
        /// Usually describes why suggestion mode is active, and what does the suggestion item represent.
        /// </summary>
        public string ToolTipText { get; }

        /// <summary>
        /// Creates instance of SuggestionItemOptions with specified tooltip text and text to display in absence of user input.
        /// Provide this instance to <see cref="CompletionContext"/> to activate suggestion mode.
        /// </summary>
        /// <param name="displayTextWhenEmpty"><see cref="CompletionItem.DisplayText"/> to use when user has not typed anything</param>
        /// <param name="toolTipText">Localized tooltip text for the suggestion item</param>
        public SuggestionItemOptions(string displayTextWhenEmpty, string toolTipText)
        {
            DisplayTextWhenEmpty = displayTextWhenEmpty ?? throw new ArgumentNullException(nameof(displayTextWhenEmpty));
            ToolTipText = toolTipText ?? throw new ArgumentNullException(nameof(toolTipText));
        }
    }
}
