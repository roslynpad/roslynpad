namespace Microsoft.VisualStudio.Editor
{
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Service for getting a service that provides common language service elements.
    /// </summary>
    /// <remarks>This class supports the Visual Studio 
    /// infrastructure and in general is not intended to be used directly from your code.</remarks>
    /// <example>
    /// This is a MEF component part. Use the code below in your MEF exported class to import an
    /// instance of the service factory.
    /// <code>
    /// [Import]
    /// private ICommonEditorAssetServiceFactory assetServiceFactory = null;
    /// </code>
    /// Then, you can use the code below to get the ITaggerProvider for the Common Editor's
    /// IClassificationTagger. Modify as needed to get the desired asset.
    /// <code>
    /// var factory = this.assetServiceFactory.GetOrCreate(buffer);
    /// var tagger = factory.FindAsset<ITaggerProvider>(
    ///     (metadata) => metadata.TagTypes.Any(tagType => typeof(IClassificationTagger).IsAssignableFrom(tagType)))
    ///     ?.CreateTagger<T>(buffer);
    /// </code>
    /// </example>
    public interface ICommonEditorAssetServiceFactory
    {
        /// <summary>
        /// Gets a service that provides common language service elements.
        /// </summary>
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> for which to initialize TextMate.</param>
        /// <remarks>
        /// This method supports the Visual Studio infrastructure and in
        /// general is not intended to be used directly from your code.
        /// </remarks>
        /// <returns>An instance of <see cref="ITextMateAssetService"/>.</returns>
        ICommonEditorAssetService GetOrCreate(ITextBuffer textBuffer);
    }
}
