using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
{
    /// <summary>
    /// Represents a class that produces instances of <see cref="ICompletionPresenter"/>
    /// </summary>
    /// <remarks>
    /// This is a MEF component and should be exported with [ContentType] and [Name] attributes
    /// and optional [Order] attribute.
    /// An instance of <see cref="ICompletionPresenterProvider"/> is selected
    /// first by matching ContentType with content type of the <see cref="ITextView.TextBuffer"/>, and then by Order.
    /// Only one <see cref="ICompletionPresenterProvider"/> is used in a given view.
    /// </remarks>
    /// <example>
    /// <code>
    ///     [Export(typeof(ICompletionPresenterProvider))]
    ///     [Name(nameof(MyCompletionPresenterProvider))]
    ///     [ContentType("any")]
    ///     [TextViewRoles(PredefinedTextViewRoles.Editable)]
    ///     [Order(Before = KnownCompletionNames.DefaultCompletionPresenter)]
    ///     public class MyCompletionPresenterProvider : ICompletionPresenterProvider
    /// </code>
    /// </example>
    public interface ICompletionPresenterProvider
    {
        /// <summary>
        /// Returns instance of <see cref="ICompletionPresenter"/> that will host completion for given <see cref="ITextView"/>.
        /// Called on the UI thread.
        /// </summary>
        /// <remarks>It is encouraged to reuse the UI over creating new UI each time this method is called.</remarks>
        /// <param name="textView">Text view that will host the completion. Completion acts on buffers of this view.</param>
        /// <returns>Instance of <see cref="ICompletionPresenter"/></returns>
        ICompletionPresenter GetOrCreate(ITextView textView);

        /// <summary>
        /// Contains additional properties of thie <see cref="ICompletionPresenter"/> that may be accessed
        /// prior to initializing an instance of <see cref="ICompletionPresenter"/>
        /// </summary>
        CompletionPresenterOptions Options { get; }
    }
}
