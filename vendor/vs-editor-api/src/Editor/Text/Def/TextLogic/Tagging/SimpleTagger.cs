//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Provides simple, thread-safe storage of and interaction with tags of the given type.
    /// </summary>
    /// <typeparam name="T">The type, which must be a subtype of <see cref="ITag"/>.</typeparam>
    public class SimpleTagger<T> : ITagger<T> where T : ITag
    {
        #region Private members

        private List<TrackingTagSpan<T>> _trackingTagSpans = new List<TrackingTagSpan<T>>();

        private ITextBuffer buffer;
        private object mutex = new object();
        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="SimpleTagger&lt;T&gt;"/> for the specified buffer.
        /// </summary>
        /// <param name="buffer">Subject buffer that will be tagged.</param>
        public SimpleTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
        }

        private int _batchNesting;
        private ITrackingSpan _batchSpan;

        private void StartBatch()
        {
            Interlocked.Increment(ref _batchNesting);
        }

        private void EndBatch()
        {
            if (Interlocked.Decrement(ref _batchNesting) == 0)
            {
                ITrackingSpan batchedRange = Interlocked.Exchange(ref _batchSpan, null);

                if (batchedRange != null)
                {
                    EventHandler<SnapshotSpanEventArgs> handler = this.TagsChanged;
                    if (handler != null)
                    {
                        handler(this, new SnapshotSpanEventArgs(batchedRange.GetSpan(buffer.CurrentSnapshot)));
                    }
                }
            }
        }

        private void UpdateBatchSpan(ITrackingSpan snapshotSpan)
        {
            ITrackingSpan newBatchSpan = snapshotSpan;

            // If there currently is a batch span, update it to include the biggest
            // range of buffer affected so far.
            if (_batchSpan != null)
            {
                ITextSnapshot snapshot = buffer.CurrentSnapshot;

                SnapshotSpan currentBatchSpan = _batchSpan.GetSpan(snapshot);
                SnapshotSpan currentUpdate = snapshotSpan.GetSpan(snapshot);

                SnapshotPoint newStart = currentBatchSpan.Start < currentUpdate.Start ? currentBatchSpan.Start : currentUpdate.Start;
                SnapshotPoint newEnd = currentBatchSpan.End > currentUpdate.End ? currentBatchSpan.End : currentUpdate.End;

                // In the event of multiple updates, we use the tracking mode of the first update's span for predictability
                newBatchSpan = snapshot.CreateTrackingSpan(new SnapshotSpan(newStart, newEnd), _batchSpan.TrackingMode);
            }

            _batchSpan = newBatchSpan;
        }

        #region SimpleTagger<T> Members

        /// <summary>
        /// Adds a tag over the given span.
        /// </summary>
        /// <param name="span">The <see cref="ITrackingSpan"/> that tracks the tag across text versions.</param>
        /// <param name="tag">The tag to associate with the given span.</param>
        /// <returns>The <see cref="TrackingTagSpan&lt;T&gt;"/> that was added, which can be used to remove the tag later on.</returns>
        /// <remarks>This method is safe to use from any thread.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> or <paramref name="tag"/> is null.</exception>
        public TrackingTagSpan<T> CreateTagSpan(ITrackingSpan span, T tag)
        {
            if (span == null)
                throw new ArgumentNullException(nameof(span));
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            var tagSpan = new TrackingTagSpan<T>(span, tag);

            StartBatch();
            try
            {
                lock (mutex)
                {
                    _trackingTagSpans.Add(tagSpan);
                    UpdateBatchSpan(tagSpan.Span);
                }
            }
            finally
            {
                EndBatch();
            }

            return tagSpan;
        }

        /// <summary>
        /// Removes a tag span that was created by calling <see cref="CreateTagSpan"/>.
        /// </summary>
        /// <param name="tagSpan">The <see cref="TrackingTagSpan&lt;T&gt;"/> returned from a previous call to <see cref="CreateTagSpan"/>.</param>
        /// <returns><c>true</c> if removed successfully, otherwise <c>false</c>.</returns>
        /// <remarks>This method is safe to use from any thread.</remarks>
        public bool RemoveTagSpan(TrackingTagSpan<T> tagSpan)
        {
            if (tagSpan == null)
                throw new ArgumentNullException(nameof(tagSpan));

            bool removed = false;

            StartBatch();
            try
            {
                lock (mutex)
                {
                    // Find the tracking tag span to be removed
                    removed = (_trackingTagSpans.Remove(tagSpan));
                    if (removed)
                    {
                        UpdateBatchSpan(tagSpan.Span);
                    }
                }
            }
            finally
            {
                EndBatch();
            }

            return removed;
        }

        /// <summary>
        /// Removes all tag spans that match the conditions specified by the predicate.
        /// </summary>
        /// <param name="match">The <see cref="Predicate&lt;T&gt;"/> that defines the match.</param>
        /// <returns>The number of tag spans removed.</returns>
        /// <remarks>This method is safe to use from any thread.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
        public int RemoveTagSpans(Predicate<TrackingTagSpan<T>> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            int removedCount = 0;

            StartBatch();
            try
            {
                lock (mutex)
                {
                    removedCount = _trackingTagSpans.RemoveAll(tagSpan =>
                    {
                        // If we have a match, then we'll need to update the batch span to include this span.
                        if (match(tagSpan))
                        {
                            UpdateBatchSpan(tagSpan.Span);
                            return true;
                        }
                        return false;
                    });
                }
            }
            finally
            {
                EndBatch();
            }

            return removedCount;
        }

        /// <summary>
        /// Gets the tagged spans that intersect the given <see cref="SnapshotSpan"/>.
        /// </summary>
        /// <param name="span">The <see cref="SnapshotSpan"/> to use.</param>
        /// <returns>The set of <see cref="TrackingTagSpan&lt;T&gt;"/> objects that intersect the given span, in order.</returns>
        public IEnumerable<TrackingTagSpan<T>> GetTaggedSpans(SnapshotSpan span)
        {
            IList<TrackingTagSpan<T>> tagSpanList;

            lock (mutex)
            {
                tagSpanList = new List<TrackingTagSpan<T>>(_trackingTagSpans);
            }

            return tagSpanList.Where(tagSpan => span.IntersectsWith(tagSpan.Span.GetSpan(span.Snapshot)));
        }

        /// <summary>
        /// Gets an IDisposable object that represents an update batch.
        /// </summary>
        /// <returns>An IDisposable object that represents an update batch.</returns>
        public IDisposable Update()
        {
            return new Batch(this);
        }

        #endregion

        #region ITagger<T> Members

        /// <summary>
        /// Gets all the tags that intersect the spans in the specified snapshot
        /// of the desired type.
        /// </summary>
        /// <param name="spans">The spans to visit.</param>
        /// <returns>A <see cref="ITagSpan&lt;T&gt;"/> for each tag.</returns>
        public IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                return Enumerable.Empty<ITagSpan<T>>();
            }

            TrackingTagSpan<T>[] tagSpanList = null;

            lock (mutex)
            {
                if (_trackingTagSpans.Count > 0)
                {
                    tagSpanList = _trackingTagSpans.ToArray();
                }
            }

            if (tagSpanList == null)
            {
                return Enumerable.Empty<ITagSpan<T>>();
            }

            return GetTagsImpl(tagSpanList, spans);

            IEnumerable<ITagSpan<T>> GetTagsImpl(TrackingTagSpan<T>[] tagSpans, NormalizedSnapshotSpanCollection querySpans)
            {
                for (int i = 0; i < tagSpans.Length; i++)
                {
                    SnapshotSpan tagSnapshotSpan = tagSpans[i].Span.GetSpan(querySpans[0].Snapshot);

                    for (int j = 0; j < querySpans.Count; j++)
                    {
                        if (tagSnapshotSpan.IntersectsWith(querySpans[j]))
                        {
                            yield return new TagSpan<T>(tagSnapshotSpan, tagSpans[i].Tag);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Occurs when one or more tags have been added or removed.
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        private class Batch : IDisposable
        {
            SimpleTagger<T> _tagger;
            internal Batch(SimpleTagger<T> tagger)
            {
                if (tagger == null)
                {
                    throw new ArgumentNullException(nameof(tagger));
                }
                _tagger = tagger;
                _tagger.StartBatch();
            }

            #region IDisposable Members

            public void Dispose()
            {
                _tagger.EndBatch();
                _tagger = null;
                GC.SuppressFinalize(this);
            }

            #endregion
        }
    }
}
