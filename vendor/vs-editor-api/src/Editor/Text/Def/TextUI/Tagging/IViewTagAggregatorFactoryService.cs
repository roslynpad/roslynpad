//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A service that creates an <see cref="ITagAggregator&lt;T&gt;"/> for an <see cref="ITextView"/>.
    /// This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IViewLevelTagAggregatorFactoryService factory = null;
    /// </summary>
    public interface IViewTagAggregatorFactoryService
    {
        /// <summary>
        /// Creates a tag aggregator for the specified <see cref="ITextView"/> that aggregates
        /// tags of the given type.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> with which to get the <see cref="ITagAggregator&lt;T&gt;"/>.</param>
        /// <typeparam name="T">The type of tag to aggregate.</typeparam>
        /// <returns>The <see cref="ITagAggregator&lt;T&gt;"/> of the correct type for <paramref name="textView"/>.</returns>
        /// <remarks>The ITagAggregatorr&lt;T&gt;.DispatchedTagsChanged event will be raised on the thread used to create the tag aggregator.</remarks>
        ITagAggregator<T> CreateTagAggregator<T>(ITextView textView) where T : ITag;

        /// <summary>
        /// Creates a tag aggregator for the specified <see cref="ITextView"/> and with the given options that aggregates
        /// tags of the given type.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> with which to get the <see cref="ITagAggregator&lt;T&gt;"/>.</param>
        /// <param name="options">The options to use for the newly created aggregator.</param>
        /// <typeparam name="T">The type of tag to aggregate.</typeparam>
        /// <returns>The <see cref="ITagAggregator&lt;T&gt;"/> of the correct type for <paramref name="textView"/>.</returns>
        ITagAggregator<T> CreateTagAggregator<T>(ITextView textView, TagAggregatorOptions options) where T : ITag;
    }
}
