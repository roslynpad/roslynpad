using System.Diagnostics;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Identifies an expander that adds <see cref="CompletionItem"/>s that reference it to the list of completions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Expander apperars in the UI alongside <see cref="CompletionFilter"/>s, but behaves differently:
    /// When <see cref="CompletionFilter"/> is selected, then <see cref="CompletionItem"/>s that don't reference it are hidden
    /// When <see cref="CompletionExpander"/> is selected, then <see cref="CompletionItem"/>s that reference it are visible
    /// When no <see cref="CompletionFilter"/>s are selected, then all <see cref="CompletionItem"/>s that don't reference an expander are visible
    /// When no <see cref="CompletionExpander"/>s are selected, then all <see cref="CompletionItem"/>s reference an expander are hidden
    /// </para>
    /// <para>
    /// These instances should be singletons. All <see cref="CompletionItem"/>s that should be filtered
    /// using the same expander button must use the same reference to the instance of <see cref="CompletionExpander"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// static CompletionExpander MyExpander = new CompletionFilter("Additional items", "a", MyAdditionalItemsImageElement);
    /// </code>
    /// </example>
    [DebuggerDisplay("+ {DisplayText}")]
    public class CompletionExpander : CompletionFilter
    {
        /// <summary>
        /// Constructs an instance of <see cref="CompletionExpander"/>.
        /// </summary>
        /// <param name="displayText">Name of this expander</param>
        /// <param name="accessKey">Key used in a keyboard shortcut that toggles this expander</param>
        /// <param name="image">Image which represents this expander</param>
        public CompletionExpander(string displayText, string accessKey, ImageElement image)
            : base(displayText, accessKey, image)
        { }
    }
}
