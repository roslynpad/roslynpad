namespace Microsoft.VisualStudio.Language.Intellisense
{
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// A MEF component part that is proffered to the IDE to construct an <see cref="IAsyncQuickInfoSource"/>.
    /// </summary>
    /// <remarks>
    /// This class is always constructed and called on the UI thread.
    /// </remarks>
    /// <example>
    /// [Export(typeof(IAsyncQuickInfoSourceProvider))]
    /// [Name("Foo QuickInfo Provider")]
    /// [Order(After = "default")]
    /// [ContentType("text")]
    /// </example>
    public interface IAsyncQuickInfoSourceProvider
    {
        /// <summary>
        /// Creates an <see cref="IAsyncQuickInfoSource"/> for the specified <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> for which this source produces items.</param>
        /// <returns>
        /// An instance of <see cref="IAsyncQuickInfoSource"/> for <paramref name="textBuffer"/>
        /// or null if no source could be created.
        /// </returns>
        IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer);
    }
}
