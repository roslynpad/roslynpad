//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Projection;

    /// <summary>
    /// Aggregates all the tag providers in a buffer graph for the specified type of tag.
    /// </summary>
    /// <typeparam name="T">The type of tag returned by the aggregator.</typeparam>
    /// <remarks>
    /// The default tag aggregator implementation also does the following:
    /// for each <see cref="ITagger&lt;T&gt;"/>  over which it aggregates tags, if the tagger is
    /// <see cref="IDisposable"/>, call Dispose() on it when the aggregator is disposed
    /// or when the taggers are dropped. For example, you should call Dispose() when 
    /// the content type of a text buffer changes or when a buffer is removed from the buffer graph.
    /// </remarks>
    public interface ITagAggregator<out T> : IDisposable where T : ITag
    {
        /// <summary>
        /// Gets all the tags that intersect the specified <paramref name="span"/> of the same type as the aggregator.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <returns>All the tags that intersect the region.</returns>
        /// <remarks>
        /// <para>The default tag aggregator lazily enumerates the tags of its <see cref="ITagger&lt;T&gt;"/> objects.
        /// Because of this, the ordering of the returned mapping spans cannot be predicted.
        /// If you need an ordered set of spans, you should collect the returned tag spans, after being mapped
        /// to the buffer of interest, into a sortable collection.</para>
        /// </remarks>
        IEnumerable<IMappingTagSpan<T>> GetTags(SnapshotSpan span);

        /// <summary>
        /// Gets all the tags that intersect the specified <paramref name="span"/> of the type of the aggregator.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <returns>All the tags that intersect the region.</returns>
        /// <remarks>
        /// <para>The default tag aggregator lazily enumerates the tags of its <see cref="ITagger&lt;T&gt;"/> objects.
        /// Because of this, the ordering of the returned mapping spans cannot be predicted.
        /// If you need an ordered set of spans, you should collect the returned tag spans, after being mapped
        /// to the buffer of interest, into a sortable collection.</para>
        /// </remarks>
        IEnumerable<IMappingTagSpan<T>> GetTags(IMappingSpan span);

        /// <summary>
        /// Gets all the tags that intersect the specified <paramref name="snapshotSpans"/> of the type of the aggregator.
        /// </summary>
        /// <param name="snapshotSpans">The spans to search.</param>
        /// <returns>All the tags that intersect the region.</returns>
        /// <remarks>
        /// <para>The default tag aggregator lazily enumerates the tags of its <see cref="ITagger&lt;T&gt;"/> objects.
        /// Because of this, the ordering of the returned mapping spans cannot be predicted.
        /// If you need an ordered set of spans, you should collect the returned tag spans, after being mapped
        /// to the buffer of interest, into a sortable collection.</para>
        /// </remarks>
        IEnumerable<IMappingTagSpan<T>> GetTags(NormalizedSnapshotSpanCollection snapshotSpans);

        /// <summary>
        /// Occurs when tags are added to or removed from providers.
        /// </summary>
        event EventHandler<TagsChangedEventArgs> TagsChanged;

        /// <summary>
        /// Occurs on idle after one or more TagsChanged events.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a batched version of the TagsChanged event.  One or more TagsChanged events 
        /// are accumulated and then raised as a single BatchedTagsChanged event on idle using the 
        /// Dispatcher.CurrentDispatcher that was active when the ITagAggregator was
        /// created. 
        /// </para>
        /// <para>
        /// This event is less noisy than TagsChanged and is always raised on the thread
        /// that was active when the ITagAggregator was created.
        /// </para>
        /// </remarks>
        event EventHandler<BatchedTagsChangedEventArgs> BatchedTagsChanged;

        /// <summary>
        /// The buffer graph over which this aggregator operates.
        /// </summary>
        IBufferGraph BufferGraph { get; }
    }
}
