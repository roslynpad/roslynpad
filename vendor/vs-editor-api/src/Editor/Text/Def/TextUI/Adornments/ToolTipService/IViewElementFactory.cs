namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Converts from an object to its equivalent platform specific UI element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type allows the same intermediate type to be rendered on different platforms through
    /// the use of platform specific exports that live in that platform's UI layer.
    /// </para>
    /// <para>
    /// You can supersede an existing <see cref="IViewElementFactory"/> for a (to, from) type
    /// pair via MEF <see cref="OrderAttribute"/>s.
    /// </para>
    /// </remarks>
    /// <example>
    /// [Export(typeof(IViewElementFactory))]
    /// [Name("object item")]
    /// [Conversion(from: typeof(object), to: typeof(UIElement))]
    /// [Order(After = "Foo", Before = "Bar")]
    /// </example>
    public interface IViewElementFactory
    {
        /// <summary>
        /// Converts <paramref name="model"/> into an equivalent object of type <typeparamref name="TView"/>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if the conversion is unknown or unsupported.</exception>
        /// <typeparam name="TView">The base type of the view element on the specific platform.</typeparam>
        /// <param name="textView">The view that owns the control that will host this view element.</param>
        /// <param name="model">The object to convert to a view element.</param>
        /// <returns>A new object of type <typeparamref name="TView"/>.</returns>
        TView CreateViewElement<TView>(ITextView textView, object model) where TView : class;
    }
}
