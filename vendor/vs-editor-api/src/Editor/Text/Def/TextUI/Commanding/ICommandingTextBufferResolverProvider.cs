namespace Microsoft.VisualStudio.Text.Editor.Commanding
{
    /// <summary>
    /// Provides a <see cref="ICommandingTextBufferResolver"/> for a given
    /// <see cref="ITextView"/> and content type.
    /// </summary>
    /// <remarks>This is a MEF component and should be exported as
    /// 
    /// Export(typeof(ICommandingTextBufferResolverProvider))]
    /// [ContentType("MyContentType")]
    /// internal class MyBufferResolverProvider : ICommandingTextBufferResolverProvider
    /// </remarks>
    public interface ICommandingTextBufferResolverProvider
    {
        /// <summary>
        /// Creates a <see cref="ICommandingTextBufferResolver"/> for a given
        /// <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">A <see cref="ITextView"/> to create a text buffer resolver for.</param>
        /// <returns>A new instance of <see cref="ICommandingTextBufferResolver"/>.</returns>
        ICommandingTextBufferResolver CreateResolver(ITextView textView);
    }
}
