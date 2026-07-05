namespace Microsoft.VisualStudio.Text.Adornments
{
    using System.Composition;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A service for converting from data objects to their platform specific UI representation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a MEF service that can be obtained via the <see cref="ImportAttribute"/> in a MEF exported class.
    /// </para>
    /// <para>
    /// The editor supports <see cref="ClassifiedTextElement"/>s, <see cref="ContainerElement"/>, <see cref="ImageElement"/>s, and <see cref="object"/>
    /// on all platforms. Text and image elements are converted to colorized text and images respectively and
    /// other objects are displayed as the <see cref="string"/> returned by <see cref="object.ToString()"/>
    /// unless an extender exports a <see cref="IViewElementFactory"/> for that type.
    /// </para>
    /// On Windows only, <see cref="ITextBuffer"/>, <see cref="ITextView"/>, and UIElement are also directly
    /// supported.
    /// </remarks>
    /// <example>
    /// [Import]
    /// internal IViewElementFactoryService viewElementFactoryService;
    /// </example>
    public interface IViewElementFactoryService
    {
        /// <summary>
        /// Converts <paramref name="model"/> into an equivalent object of type <typeparamref name="TView"/>.
        /// </summary>
        /// <typeparam name="TView">The base type of the view element on the specific platform.</typeparam>
        /// <param name="textView">The textView that owns the control that will host this view element.</param>
        /// <param name="model">The object to convert to a view element.</param>
        /// <returns>A new object of type <typeparamref name="TView"/> or null if the conversion is unknown.</returns>
        TView CreateViewElement<TView>(ITextView textView, object model) where TView : class;
    }
}
