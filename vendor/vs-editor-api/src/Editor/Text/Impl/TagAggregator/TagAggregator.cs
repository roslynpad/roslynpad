//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Tagging.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// A tag aggregator gathers, projects, and aggregates tags over a given buffer graph.  Consumers
    /// of tags will get a TagAggregator for a view (likely) or a specific buffer (less likely) and
    /// query for tags over this aggregator.  When finished, consumers are expected to Dispose the
    /// aggregator, so it can clean up any taggers that are disposable and any cached state it may
    /// have.
    /// </summary>
    internal sealed class TagAggregator<T> : IAccurateTagAggregator<T> where T : ITag
    {
        internal TagAggregatorFactoryService TagAggregatorFactoryService { get; private set; }
        internal readonly IDictionary<ITextBuffer, BufferState> bufferStates = new Dictionary<ITextBuffer, BufferState>();
        private readonly TagAggregatorOptions options;
        private readonly IDictionary<ITagger<T>, BoxedInt> uniqueTaggers = new Dictionary<ITagger<T>, BoxedInt>();
        internal ITextView textView;    // can be null
        internal JoinableTaskHelper joinableTaskHelper;

        internal MappingSpanLink acculumatedSpanLinks = null;

        internal bool disposed;
        internal bool initialized;
        internal int versionNumber = 0;

        public TagAggregator(TagAggregatorFactoryService factory, ITextView textView, IBufferGraph bufferGraph, TagAggregatorOptions options)
        {
            this.TagAggregatorFactoryService = factory;
            this.textView = textView;
            this.BufferGraph = bufferGraph;
            this.options = options;
            this.joinableTaskHelper = new JoinableTaskHelper(factory.JoinableTaskContext);

            if (textView != null)
            {
                textView.Closed += this.OnTextView_Closed;
            }

            if (((TagAggregatorOptions2)options).HasFlag(TagAggregatorOptions2.DeferTaggerCreation))
            {
                this.joinableTaskHelper.RunOnUIThread((Action)(this.EnsureInitialized));
            }
            else
            {
                this.Initialize();
            }
        }

        private void Initialize()
        {
            this.RegisterBufferGraph();
            this.initialized = true;
        }

        private void EnsureInitialized()
        {
            if (!(this.disposed || this.initialized))
            {
                this.Initialize();

                //Raise the tags changed event over the entire buffer since we didn't give the correct results
                //to anyone who might have called GetTags() before.
                ITextSnapshot snapshot = this.BufferGraph.TopBuffer.CurrentSnapshot;
                IMappingSpan span = this.BufferGraph.CreateMappingSpan(new SnapshotSpan(snapshot, 0, snapshot.Length), SpanTrackingMode.EdgeInclusive);

                this.RaiseEvents(this, span);
            }
        }

        #region ITagAggregator<T> Members

        public IBufferGraph BufferGraph { get; private set; }

        public IEnumerable<IMappingTagSpan<T>> GetTags(SnapshotSpan span)
        {
            if (this.disposed)
                throw new ObjectDisposedException("TagAggregator");

            if (initialized && (uniqueTaggers.Count > 0))
            {
                return InternalGetTags(new NormalizedSnapshotSpanCollection(span), cancel: null);
            }

            return Enumerable.Empty<IMappingTagSpan<T>>();
        }

        public IEnumerable<IMappingTagSpan<T>> GetTags(IMappingSpan span)
        {
            if (span == null)
                throw new ArgumentNullException(nameof(span));

            if (this.disposed)
                throw new ObjectDisposedException("TagAggregator");

            if (initialized && (uniqueTaggers.Count > 0))
            {
                return InternalGetTags(span, cancel: null);
            }

            return Enumerable.Empty<IMappingTagSpan<T>>();
        }

        public IEnumerable<IMappingTagSpan<T>> GetTags(NormalizedSnapshotSpanCollection snapshotSpans)
        {
            if (this.disposed)
                throw new ObjectDisposedException("TagAggregator");

            if (initialized && (uniqueTaggers.Count > 0) && (snapshotSpans.Count > 0))
            {
                return InternalGetTags(snapshotSpans, cancel: null);
            }

            return Enumerable.Empty<IMappingTagSpan<T>>();
        }

        public event EventHandler<TagsChangedEventArgs> TagsChanged;

        public event EventHandler<BatchedTagsChangedEventArgs> BatchedTagsChanged;

        #endregion

        #region IAccurateTagAggregator<T> Members

        public IEnumerable<IMappingTagSpan<T>> GetAllTags(SnapshotSpan span, CancellationToken cancel)
        {
            if (this.disposed)
                throw new ObjectDisposedException("TagAggregator");

            this.EnsureInitialized();
            if (uniqueTaggers.Count > 0)
                return InternalGetTags(new NormalizedSnapshotSpanCollection(span), cancel);

            return Enumerable.Empty<IMappingTagSpan<T>>();
        }

        public IEnumerable<IMappingTagSpan<T>> GetAllTags(IMappingSpan span, CancellationToken cancel)
        {
            if (span == null)
                throw new ArgumentNullException(nameof(span));

            if (this.disposed)
                throw new ObjectDisposedException("TagAggregator");

            this.EnsureInitialized();
            if (uniqueTaggers.Count > 0)
                return InternalGetTags(span, cancel);

            return Enumerable.Empty<IMappingTagSpan<T>>();
        }

        public IEnumerable<IMappingTagSpan<T>> GetAllTags(NormalizedSnapshotSpanCollection snapshotSpans, CancellationToken cancel)
        {
            if (this.disposed)
                throw new ObjectDisposedException("TagAggregator");

            this.EnsureInitialized();

            if ((uniqueTaggers.Count > 0) && (snapshotSpans.Count > 0))
            {
                return InternalGetTags(snapshotSpans, cancel);
            }

            return Enumerable.Empty<IMappingTagSpan<T>>();
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            if (this.disposed)
                return;

            try
            {
                if (this.textView != null)
                    this.textView.Closed -= this.OnTextView_Closed;

                foreach (var bufferAndTaggers in bufferStates)
                {
                    this.UnregisterBuffer(bufferAndTaggers.Key, bufferAndTaggers.Value);
                }

                Debug.Assert(this.uniqueTaggers.Count == 0);
            }
            finally
            {
                this.bufferStates.Clear();
                this.uniqueTaggers.Clear();
                this.TagAggregatorFactoryService = null;
                this.BufferGraph = null;
                this.textView = null;

                disposed = true;
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// When a source tagger sends out a change event, we translate the SnapshotSpan
        /// that was changed into a mapping span for our consumers.
        /// </summary>
        void SourceTaggerTagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            if (this.disposed)
                return;

            // Create a mapping span for the region and return that in our own event
            IMappingSpan span = this.BufferGraph.CreateMappingSpan(e.Span, SpanTrackingMode.EdgeExclusive);

            RaiseEvents(sender, span);
        }

        private void RaiseEvents(object sender, IMappingSpan span)
        {
            EventHandler<TagsChangedEventArgs> tempEvent = TagsChanged;
            if (tempEvent != null)
            {
                this.TagAggregatorFactoryService.GuardedOperations.RaiseEvent(sender, tempEvent, new TagsChangedEventArgs(span));
            }

            if (this.BatchedTagsChanged != null)
            {
                var oldHead = Volatile.Read(ref this.acculumatedSpanLinks);
                while (true)
                {
                    var newHead = new MappingSpanLink(oldHead, span);
                    var result = Interlocked.CompareExchange(ref this.acculumatedSpanLinks, newHead, oldHead);
                    if (result == oldHead)
                    {
                        if (oldHead == null)
                        {
                            this.joinableTaskHelper.RunOnUIThread((Action)(this.RaiseBatchedTagsChanged));
                        }

                        break;
                    }

                    oldHead = result;
                }
            }
        }

        private void RaiseBatchedTagsChanged()
        {
            // We may have been disposed between when the event was
            // dispatched and now; if so, just quit.
            if (this.disposed)
                return;

            bool raiseEvent = true;

            EventHandler<BatchedTagsChangedEventArgs> tempEvent = this.BatchedTagsChanged;
            if (tempEvent != null)
            {
                if (this.textView != null)
                {
                    if (this.textView.IsClosed)
                    {
                        // There's no need to actually raise the event (this probably won't happen since -- with a closed view -- there shouldn't be any listeners).
                        raiseEvent = false;
                    }
                    else if (this.textView.InLayout)
                    {
                        // The view is in the middle of a layout (because someone was pumping messages while handling a call from inside a layout).
                        // Many BatchTagsChanged handlers will not handle that situation gracefully so simply delay raising the event until
                        // we're no longer inside a layout.
                        this.joinableTaskHelper.RunOnUIThread((Action)(this.RaiseBatchedTagsChanged));

                        return;
                    }
                }
            }
            else
            {
                raiseEvent = false;
            }

            var oldHead = Volatile.Read(ref this.acculumatedSpanLinks);
            while (true)
            {
                var result = Interlocked.CompareExchange(ref this.acculumatedSpanLinks, null, oldHead);
                if (result == oldHead)
                {
                    if (raiseEvent)
                    {
                        var spans = new List<IMappingSpan>(oldHead.Count);
                        do
                        {
                            spans.Add(oldHead.Span);
                            oldHead = oldHead.Next;
                        }
                        while (oldHead != null);

                        this.TagAggregatorFactoryService.GuardedOperations.RaiseEvent(this, tempEvent, new BatchedTagsChangedEventArgs(spans));
                    }

                    break;
                }

                oldHead = result;
            }
        }

        internal class MappingSpanLink
        {
            public readonly MappingSpanLink Next;
            public readonly IMappingSpan Span;
            public int Count { get { return (this.Next == null) ? 1 : (this.Next.Count + 1); } }

            public MappingSpanLink(MappingSpanLink next, IMappingSpan span)
            {
                this.Next = next;
                this.Span = span;
            }
        }

        private void OnTextView_Closed(object sender, EventArgs args)
        {
            this.Dispose();
        }
        #endregion

        #region Helpers
        private IEnumerable<IMappingTagSpan<T>> GetTagsForBuffer(NormalizedSnapshotSpanCollection snapshotSpans,
                                                                 ITextSnapshot root, CancellationToken? cancel)
        {
            ITextSnapshot snapshot = snapshotSpans[0].Snapshot;
            if (this.bufferStates.TryGetValue(snapshot.TextBuffer, out BufferState taggersForBuffer))
            {
                return this.GetTagsForBuffer(snapshotSpans, taggersForBuffer, root, cancel);
            }
            else
            {
                return Array.Empty<IMappingTagSpan<T>>();
            }
        }

        private IEnumerable<IMappingTagSpan<T>> GetTagsForBuffer(NormalizedSnapshotSpanCollection snapshotSpans,
                                                                 BufferState taggersForBuffer, ITextSnapshot root, CancellationToken? cancel)
        {
            ITextSnapshot snapshot = snapshotSpans[0].Snapshot;
            for (int t = 0; t < taggersForBuffer.Taggers.Count; ++t)
            {
                ITagger<T> tagger = taggersForBuffer.Taggers[t];
                IEnumerator<ITagSpan<T>> tags = null;
                try
                {
                    IEnumerable<ITagSpan<T>> tagEnumerable;

                    if (cancel.HasValue)
                    {
                        cancel.Value.ThrowIfCancellationRequested();

                        var tagger2 = tagger as IAccurateTagger<T>;
                        if (tagger2 != null)
                        {
                            tagEnumerable = tagger2.GetAllTags(snapshotSpans, cancel.Value);
                        }
                        else
                        {
                            tagEnumerable = tagger.GetTags(snapshotSpans);
                        }
                    }
                    else
                    {
                        tagEnumerable = tagger.GetTags(snapshotSpans);
                    }

                    if (tagEnumerable != null)
                        tags = tagEnumerable.GetEnumerator();
                }
                catch (OperationCanceledException)
                {
                    // Rethrow cancellation exceptions since we expect our callers to deal with it.
                    throw;
                }
                catch (Exception e)
                {
                    this.TagAggregatorFactoryService.GuardedOperations.HandleException(tagger, e);
                }

                if (tags != null)
                {
                    try
                    {
                        while (true)
                        {
                            ITagSpan<T> tagSpan = null;
                            try
                            {
                                if (tags.MoveNext())
                                    tagSpan = tags.Current;
                            }
                            catch (Exception e)
                            {
                                this.TagAggregatorFactoryService.GuardedOperations.HandleException(tagger, e);
                            }

                            if (tagSpan == null)
                                break;

                            var snapshotSpan = tagSpan.Span;

                            if (snapshotSpans.IntersectsWith(snapshotSpan.TranslateTo(snapshot, SpanTrackingMode.EdgeExclusive)))
                            {
                                yield return new MappingTagSpan<T>(
                                    (root == null)
                                    ? this.BufferGraph.CreateMappingSpan(snapshotSpan, SpanTrackingMode.EdgeExclusive)
                                    : MappingSpanSnapshot.Create(root, snapshotSpan, SpanTrackingMode.EdgeExclusive, this.BufferGraph),
                                    tagSpan.Tag);
                            }
                            else
                            {
#if DEBUG
                                Debug.WriteLine("tagger provided an extra (non-intersecting) tag at " + snapshotSpan + " when queried for tags over " + snapshotSpans);
#endif
                            }
                        }
                    }
                    finally
                    {
                        try
                        {
                            tags.Dispose();
                        }
                        catch (Exception e)
                        {
                            this.TagAggregatorFactoryService.GuardedOperations.HandleException(tagger, e);
                        }
                    }
                }
            }
        }

        private IEnumerable<IMappingTagSpan<T>> InternalGetTags(NormalizedSnapshotSpanCollection snapshotSpans, CancellationToken? cancel)
        {
            bool mapByContentType = (options & TagAggregatorOptions.MapByContentType) != 0;
            var sourceSnapshot = snapshotSpans[0].Snapshot as IProjectionSnapshot;
            if ((sourceSnapshot != null) && ((!mapByContentType) || sourceSnapshot.TextBuffer.ContentType.IsOfType("projection")))
            {
                var allSpans = new Dictionary<ITextSnapshot, IList<Span>>();
                allSpans.Add(sourceSnapshot, (NormalizedSpanCollection)snapshotSpans);

                for (int i = 0; (i < snapshotSpans.Count); ++i)
                {
                    ExtendSourceMap(sourceSnapshot, snapshotSpans[i], mapByContentType, allSpans);
                }

                return this.GetTagsForBuffers(allSpans, sourceSnapshot, cancel);
            }
            else
            {
                return this.GetTagsForBuffer(snapshotSpans, snapshotSpans[0].Snapshot, cancel);
            }
        }

        private static void ExtendSourceMap(IProjectionSnapshot sourceSnapshot, Span sourceSpan, bool mapByContentType, Dictionary<ITextSnapshot, IList<Span>> allSpans)
        {
            var childSpans = sourceSnapshot.MapToSourceSnapshots(sourceSpan);
            for (int c = 0; (c < childSpans.Count); ++c)
            {
                var childSpan = childSpans[c];
                if (!allSpans.TryGetValue(childSpan.Snapshot, out IList<Span> spans))
                {
                    spans = new FrugalList<Span>();
                    allSpans.Add(childSpan.Snapshot, spans);
                }

                spans.Add(childSpan);

                if ((childSpan.Snapshot is IProjectionSnapshot childProjectionSnapshot) &&
                    (!mapByContentType || childProjectionSnapshot.TextBuffer.ContentType.IsOfType("projection")))
                {
                    ExtendSourceMap(childProjectionSnapshot, childSpan, mapByContentType, allSpans);
                }
            }
        }

        private IEnumerable<IMappingTagSpan<T>> GetTagsForBuffers(Dictionary<ITextSnapshot, IList<Span>> allSpans, ITextSnapshot root, CancellationToken? cancel)
        {
            foreach (var kvp in allSpans)
            {
                var spans = new NormalizedSnapshotSpanCollection(kvp.Key, kvp.Value);
                foreach (var t in this.GetTagsForBuffer(spans, root, cancel))
                    yield return t;

                if (cancel.HasValue && cancel.Value.IsCancellationRequested)
                    yield break;
            }
        }

        private IEnumerable<IMappingTagSpan<T>> InternalGetTags(IMappingSpan mappingSpan, CancellationToken? cancel)
        {
            foreach (var bufferAndState in bufferStates)
            {
                if (bufferAndState.Value.Taggers.Count > 0)
                {
                    var spans = mappingSpan.GetSpans(bufferAndState.Key);
                    if (spans.Count > 0)
                    {
                        foreach (var tag in this.GetTagsForBuffer(spans, bufferAndState.Value, null, cancel))
                        {
                            yield return tag;
                        }

                        if (cancel.HasValue && cancel.Value.IsCancellationRequested)
                            yield break;
                    }
                }
            }
        }

        void DisposeAllTaggersOverBuffer(IList<ITagger<T>> taggersOnBuffer)
        {
            foreach (ITagger<T> tagger in taggersOnBuffer)
            {
                this.UnregisterTagger(tagger);
            }
        }

        internal void RegisterBuffer(ITextBuffer textBuffer)
        {
            if (this.bufferStates.TryGetValue(textBuffer, out BufferState state))
            {
                // The buffer is already registered, bumps its version number to the current version.
                state.VersionNumber = this.versionNumber;
            }
            else
            {
                textBuffer.ContentTypeChanged += OnContentTypeChanged;
                if (textBuffer is IProjectionBuffer projection)
                {
                    projection.SourceBuffersChanged += OnSourceBuffersChanged;
                }

                this.bufferStates.Add(textBuffer, new BufferState(this.versionNumber, this.GatherTaggers(textBuffer)));
            }
        }

        internal IList<ITagger<T>> GatherTaggers(ITextBuffer textBuffer)
        {
            var newTaggers = new List<ITagger<T>>();
            this.AddTaggers(textBuffer, newTaggers);
            return newTaggers;
        }

        internal void AddTaggers(ITextBuffer textBuffer, IList<ITagger<T>> newTaggers)
        {
            var bufferTaggerFactories = this.TagAggregatorFactoryService.GuardedOperations.FindEligibleFactories(this.TagAggregatorFactoryService.GetBufferTaggersForType(textBuffer.ContentType, typeof(T)),
                                                                                                     textBuffer.ContentType,
                                                                                                     this.TagAggregatorFactoryService.ContentTypeRegistryService);

            foreach (var factory in bufferTaggerFactories)
            {
                ITaggerProvider provider = null;
                ITagger<T> tagger = null;

                try
                {
                    provider = factory.Value;
                    tagger = provider.CreateTagger<T>(textBuffer);
                }
                catch (Exception e)
                {
                    object errorSource = (provider != null) ? (object)provider : factory;
                    this.TagAggregatorFactoryService.GuardedOperations.HandleException(errorSource, e);
                }

                this.RegisterTagger(tagger, newTaggers);
            }

            if (this.textView != null)
            {
                var viewTaggerFactories = this.TagAggregatorFactoryService.GuardedOperations.FindEligibleFactories(this.TagAggregatorFactoryService.GetViewTaggersForType(textBuffer.ContentType, typeof(T)).Where(f =>
                                                                                                                           (f.Metadata.TextViewRoles == null) || this.textView.Roles.ContainsAny(f.Metadata.TextViewRoles)),
                                                                                                                   textBuffer.ContentType,
                                                                                                                   this.TagAggregatorFactoryService.ContentTypeRegistryService);

                foreach (var factory in viewTaggerFactories)
                {
                    IViewTaggerProvider provider = null;
                    ITagger<T> tagger = null;

                    try
                    {
                        provider = factory.Value;
                        tagger = provider.CreateTagger<T>(this.textView, textBuffer);
                    }
                    catch (Exception e)
                    {
                        object errorSource = (provider != null) ? (object)provider : factory;
                        this.TagAggregatorFactoryService.GuardedOperations.HandleException(errorSource, e);
                    }

                    this.RegisterTagger(tagger, newTaggers);
                }
            }
        }

        internal void UnregisterAndRemoveBuffer(ITextBuffer buffer)
        {
            if (this.bufferStates.TryGetValue(buffer, out BufferState t))
            {
                this.bufferStates.Remove(buffer);
                this.UnregisterBuffer(buffer, t);
            }
        }

        internal void UnregisterBuffer(ITextBuffer buffer, BufferState state)
        {
            buffer.ContentTypeChanged -= OnContentTypeChanged;
            if (buffer is IProjectionBuffer projection)
            {
                projection.SourceBuffersChanged -= OnSourceBuffersChanged;
            }

            foreach (var tagger in state.Taggers)
            {
                this.UnregisterTagger(tagger);
            }
            state.Taggers.Clear();
        }

        private void OnSourceBuffersChanged(object sender, ProjectionSourceBuffersChangedEventArgs e)
        {
            // This is something of a hack. We can't use the buffer graph events for tracking when buffers are added
            // or removed from the projection stack (the buffer events are fired after the corresponding text changed
            // events so people getting tags inside the text change event would not see any tags on the newly added
            // buffers). Instead, we update our buffer states by traversing the entire buffer graph whenever the
            // source buffers for any projection buffer in the graph changes.
            //
            // This should be rare.

            // Bump our version number so we can tell which BufferStates are unused.
            ++(this.versionNumber);

            this.RegisterBufferGraph();

            // Now that all living buffers have been registerd (and had their version numbers bumped), find any stale buffers
            // that have an old version number and remove them.
            List<ITextBuffer> deadBuffers = null;
            foreach (var kvp in this.bufferStates)
            {
                if (kvp.Value.VersionNumber != this.versionNumber)
                {
                    if (deadBuffers == null)
                        deadBuffers = new List<ITextBuffer>(this.bufferStates.Count);
                    deadBuffers.Add(kvp.Key);
                }
            }

            if (deadBuffers != null)
            {
                foreach (var b in deadBuffers)
                {
                    this.UnregisterAndRemoveBuffer(b);
                }
            }
        }

        private void RegisterBufferGraph()
        {
            if (((TagAggregatorOptions2)this.options).HasFlag(TagAggregatorOptions2.NoProjection))
            {
                this.RegisterBuffer(this.BufferGraph.TopBuffer);
            }
            else
            {
                //Construct our initial list of taggers by getting taggers for every textBuffer in the graph
                this.RegisterSnapshotAndChildren(this.BufferGraph.TopBuffer.CurrentSnapshot);
            }
        }

        private void RegisterSnapshotAndChildren(ITextSnapshot snapshot)
        {
            this.RegisterBuffer(snapshot.TextBuffer);
            if (snapshot is IProjectionSnapshot projection)
            {
                foreach (var child in projection.SourceSnapshots)
                {
                    this.RegisterSnapshotAndChildren(child);
                }
            }
        }

        private void OnContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            // It is possible we have a situation where we are contained in another tagger. We both subscribe to the buffers content type changed event
            // but the other tagger is disposed of first (and it disposes of us) so we need to guard against handling the event after being disposed of.
            if (!this.disposed)
            {
                if (this.bufferStates.TryGetValue(e.After.TextBuffer, out BufferState state))
                {
                    foreach (var tagger in state.Taggers)
                    {
                        this.UnregisterTagger(tagger);
                    }

                    state.Taggers.Clear();
                    this.AddTaggers(e.After.TextBuffer, state.Taggers);
                }

                // Send out an event to say that tags have changed over the entire text buffer, to
                // be safe.
                SnapshotSpan entireSnapshot = new SnapshotSpan(e.After, 0, e.After.Length);
                IMappingSpan span = this.BufferGraph.CreateMappingSpan(entireSnapshot, SpanTrackingMode.EdgeInclusive);

                this.RaiseEvents(this, span);
            }
        }

        private void UnregisterTagger(ITagger<T> tagger)
        {
            if (this.uniqueTaggers.TryGetValue(tagger, out var count))
            {
                if (--(count.Value) == 0)
                {
                    tagger.TagsChanged -= SourceTaggerTagsChanged;
                    this.uniqueTaggers.Remove(tagger);
                }
            }
            else
            {
                Debug.Fail("The tagger should still be in the list of unique taggers.");
            }

            // Note we are intentionally disposing of the object even if it continues to live on in uniqueTaggers. We'd only
            // get that situation if two different tagger providers returned the same tagger (and, if that tagger implements
            // IDisposable, then the expectation is that Dispose would be called for one for each time the tagger was "created").
            IDisposable disposable = tagger as IDisposable;
            if (disposable != null)
            {
                this.TagAggregatorFactoryService.GuardedOperations.CallExtensionPoint(this, () => disposable.Dispose());
            }
        }

        private void RegisterTagger(ITagger<T> tagger, IList<ITagger<T>> newTaggers)
        {
            if (tagger != null)
            {
                newTaggers.Add(tagger);
                if (!this.uniqueTaggers.TryGetValue(tagger, out var count))
                {
                    count = new BoxedInt();
                    this.uniqueTaggers.Add(tagger, count);

                    // We only want to subscribe once to the tags changed event
                    // (even if we get multiple instances of the same tagger).
                    tagger.TagsChanged += SourceTaggerTagsChanged;
                }

                ++(count.Value);
            }
        }

        class BoxedInt
        {
            public int Value;
        }

        internal class BufferState
        {
            public int VersionNumber;
            public IList<ITagger<T>> Taggers;

            public BufferState(int versionNumber, IList<ITagger<T>> taggers)
            {
                this.VersionNumber = versionNumber;
                this.Taggers = taggers;
            }
        }
        #endregion
    }
}
