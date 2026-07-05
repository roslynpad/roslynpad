using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Provides instances of <see cref="IAsyncCompletionCommitManager"/> which provides means to adjust the commit behavior,
    /// including which typed characters commit the <see cref="IAsyncCompletionSession"/>
    /// and how to commit <see cref="CompletionItem"/>s.
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
    ///     [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    ///     [Name(nameof(MyCompletionCommitManagerProvider))]
    ///     [ContentType("text")]
    ///     [TextViewRoles(PredefinedTextViewRoles.Editable)]
    ///     [Order(Before = "OtherCompletionCommitManager")]
    ///     public class MyCompletionCommitManagerProvider : IAsyncCompletionCommitManagerProvider
    /// </code>
    /// </example>
    public interface IAsyncCompletionCommitManagerProvider
    {
        /// <summary>
        /// Creates an instance of <see cref="IAsyncCompletionCommitManager"/> for the specified <see cref="ITextView"/>.
        /// Called on the UI thread.
        /// </summary>
        /// <param name="textView">Text view that will host the completion. Completion acts on buffers of this view.</param>
        /// <returns>Instance of <see cref="IAsyncCompletionItemManager"/></returns>
        IAsyncCompletionCommitManager GetOrCreate(ITextView textView);
    }
}
