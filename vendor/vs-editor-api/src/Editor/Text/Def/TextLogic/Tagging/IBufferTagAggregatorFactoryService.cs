//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// A factory that creates an <see cref="ITagAggregator&lt;T&gt;"/> for an <see cref="ITextBuffer"/>.
    /// </summary>
    /// <remarks>This is a MEF component part, and implementations should use the following to import it:
    /// [Import]
    /// IBufferTagAggregatorFactoryService factory = null;
    /// </remarks>
    public interface IBufferTagAggregatorFactoryService
    {
        /// <summary>
        /// Creates a tag aggregator for a <paramref name="textBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> over which the aggregator should aggregate tags,
        /// including all source buffers if the buffer is a projection buffer.</param>
        /// <typeparam name="T">The type of tag to aggregate.</typeparam>
        /// <returns>The tag aggregator for <paramref name="textBuffer"/>.</returns>
        /// <remarks>The ITagAggregatorr&lt;T&gt;.DispatchedTagsChanged event will be raised on the thread used to create the tag aggregator.</remarks>
        ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer) where T : ITag;

        /// <summary>
        /// Creates a tag aggregator for a <paramref name="textBuffer"/>, using the given options.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> over which the aggregator should aggregate tags,
        /// including all source buffers if the buffer is a projection buffer.</param>
        /// <param name="options">The options to use for the newly created aggregator.</param>
        /// <typeparam name="T">The type of tag to aggregate.</typeparam>
        /// <returns>The tag aggregator for <paramref name="textBuffer"/>.</returns>
        ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer, TagAggregatorOptions options) where T : ITag;
    }
}
