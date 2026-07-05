//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(ITaggerProvider))]
    [ContentType("projection")]
    [TagType(typeof(ClassificationTag))]
    [Shared]
    public class ProjectionWorkaroundProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            IProjectionBuffer projectionBuffer = buffer as IProjectionBuffer;
            if (projectionBuffer == null)
                return null;

            return new ProjectionWorkaroundTagger(projectionBuffer) as ITagger<T>;
        }
    }

    /// <summary>
    /// This is a workaround for projection buffers.  Currently, the way our projection buffer
    /// implementation does "minimal" updating is by using *lexical* differencing.  The
    /// problem with this approach is that if you replace a span in a projection buffer
    /// with a lexically equivalent span from a *different* buffer, the projection buffer
    /// will event that no changes have been made.
    /// </summary>
    internal class ProjectionWorkaroundTagger : ITagger<ClassificationTag>
    {
        IProjectionBuffer ProjectionBuffer { get; set; }

        internal ProjectionWorkaroundTagger(IProjectionBuffer projectionBuffer)
        {
            this.ProjectionBuffer = projectionBuffer;
            this.ProjectionBuffer.SourceBuffersChanged += SourceSpansChanged;
        }

        #region ITagger<ClassificationTag> members
        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return Array.Empty<ITagSpan<ClassificationTag>>();
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        #endregion

        #region Source span differencing + change event
        private void SourceSpansChanged(object sender, ProjectionSourceSpansChangedEventArgs e)
        {
            var handler = TagsChanged;
            if ((handler != null) && (e.Changes.Count == 0))
            {
                // If there weren't text changes, but there were span changes, then
                // send out a classification changed event over the spans that changed.
                //
                // We're raising a single event here so all we need is the start of the first changed span
                // to the end of the last changed span (or, as we calculate it, the end of the first identical
                // spans to the start of the last identical spans).
                //
                // Note that we are being generous in the span we raise. For example if I change the projection buffer
                // from projecting (V0:[0,10)) and (V0:[10,15)) to projecting (V0:[0,5)) and (V0:[5,15)) we'll raise a snapshot changed
                // event over the entire buffer even though neither the projected text nor the content type of its buffer
                // changed. This case shouldn't happen very often and the cost of (falsely) raising a classification changed
                // event is pretty small so this is a net perf win compared to doing a more expensive diff to get the actual
                // changed span.
                var leftSpans = e.Before.GetSourceSpans();
                var rightSpans = e.After.GetSourceSpans();
                var spansToCompare = Math.Min(leftSpans.Count, rightSpans.Count);

                int start = 0;
                int identicalSpansAtStart = 0;
                while ((identicalSpansAtStart < spansToCompare) && (leftSpans[identicalSpansAtStart] == rightSpans[identicalSpansAtStart]))
                {
                    start += rightSpans[identicalSpansAtStart].Length;
                    ++identicalSpansAtStart;
                }

                if ((identicalSpansAtStart < leftSpans.Count) || (identicalSpansAtStart < rightSpans.Count))
                {
                    // There are at least some span differences between leftSpans and rightSpans so we don't need to worry about running over.
                    spansToCompare -= identicalSpansAtStart;    //No need to compare spans in the starting identical block.
                    int end = e.After.Length;
                    int identicalSpansAtEndPlus1 = 1;
                    while ((identicalSpansAtEndPlus1 <= spansToCompare) && (leftSpans[leftSpans.Count - identicalSpansAtEndPlus1] == rightSpans[rightSpans.Count - identicalSpansAtEndPlus1]))
                    {
                        end -= rightSpans[rightSpans.Count - identicalSpansAtEndPlus1].Length;
                        ++identicalSpansAtEndPlus1;
                    }

                    handler(this, new SnapshotSpanEventArgs(new SnapshotSpan(e.After, Span.FromBounds(start, end))));
                }
            }
        }
        #endregion
    }
}
