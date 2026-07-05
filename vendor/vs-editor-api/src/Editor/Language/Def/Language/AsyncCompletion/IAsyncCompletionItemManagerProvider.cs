using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Provides instances of <see cref="IAsyncCompletionItemManager"/> which filters and sorts available <see cref="CompletionItem"/>s given the current state of the editor.
    /// </summary>
    /// <remarks>
    /// This is a MEF component and should be exported with [ContentType] and [Name] attributes
    /// and optional [Order] and [TextViewRoles] attributes.
    /// An instance of <see cref="IAsyncCompletionItemManager"/> is selected
    /// first by matching ContentType with content type of the <see cref="ITextView.TextBuffer"/>, and then by Order.
    /// Only one <see cref="IAsyncCompletionItemManager"/> is used in a given view.
    /// </remarks>
    /// <example>
    /// <code>
    ///     [Export(typeof(IAsyncCompletionItemManagerProvider))]
    ///     [Name(nameof(MyCompletionItemManagerProvider))]
    ///     [ContentType("text")]
    ///     [TextViewRoles(PredefinedTextViewRoles.Editable)]
    ///     [Order(Before = "OtherCompletionItemManager")]
    ///     public class MyCompletionItemManagerProvider : IAsyncCompletionItemManagerProvider
    /// </code>
    /// </example>
    public interface IAsyncCompletionItemManagerProvider
    {
        /// <summary>
        /// Creates an instance of <see cref="IAsyncCompletionItemManager"/> for the specified <see cref="ITextView"/>.
        /// Called on the UI thread.
        /// </summary>
        /// <param name="textView">Text view that will host the completion</param>
        /// <returns>Instance of <see cref="IAsyncCompletionItemManager"/> that will sort and filter <see cref="CompletionItem"/>s</returns>
        IAsyncCompletionItemManager GetOrCreate(ITextView textView);
    }
}
