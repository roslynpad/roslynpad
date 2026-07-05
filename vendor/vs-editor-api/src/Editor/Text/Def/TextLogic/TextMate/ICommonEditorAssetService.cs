namespace Microsoft.VisualStudio.Editor
{
    using System;

    /// <summary>
    /// Service produced by <see cref="ICommonEditorAssetServiceFactory"/> that provides common language service assets.
    /// </summary>
    /// <remarks>This class supports the Visual Studio 
    /// infrastructure and in general is not intended to be used directly from your code.</remarks>
    public interface ICommonEditorAssetService
    {
        /// <summary>
        /// Produces common language service asset.
        /// </summary>
        /// <typeparam name="T">
        /// The type of language service asset to produce. Can be ITaggerProvider, IViewTaggerProvider,
        /// or ICompletionSource. Use <paramref name="isMatch"/> to find a tagger of the desired type.
        /// </typeparam>
        /// <param name="isMatch">Returns true if the <see cref="ICommonEditorAssetMetadata"/> matches the desired feature.</param>
        /// <remarks>
        /// This method supports the Visual Studio infrastructure and in
        /// general is not intended to be used directly from your code.
        /// </remarks>
        /// <returns>A feature of <typeparamref name="T"/> or null if unknown.</returns>
        T FindAsset<T>(Predicate<ICommonEditorAssetMetadata> isMatch = null) where T : class;
    }
}
