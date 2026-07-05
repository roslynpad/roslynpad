//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Microsoft.VisualStudio.Text.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;

    using Strings = Microsoft.VisualStudio.Text.Implementation.Strings;
    using System.Globalization;

    internal partial class ProjectionSnapshot : BaseProjectionSnapshot, IProjectionSnapshot
    {
        #region Private members
        private readonly ProjectionBuffer projectionBuffer;
        private ReadOnlyCollection<SnapshotSpan> sourceSpans;
        private ReadOnlyCollection<ITextSnapshot> sourceSnapshots;


        /// <summary>
        /// Denotes a source span and its character position in the projection snapshot.
        /// </summary>
        struct InvertedSource
        {
            public readonly Span sourceSpan;
            public readonly int projectedPosition;
            public InvertedSource(Span sourceSpan, int projectedPosition)
            {
                this.sourceSpan = sourceSpan;
                this.projectedPosition = projectedPosition;
            }
        }

        /// <summary>
        /// For each source snapshot, a list of the source spans from that snapshot, plus their positions in the
        /// projection snapshot, ordered by their positions in the source snapshot. This enables fast mapping from 
        /// source snapshot positions/spans to the equivalent in this projection snapshot. For projection buffers
        /// with many source spans, the (time) overhead of creating this structure is about 10% of the snapshot cost.
        /// </summary>
        private Dictionary<ITextSnapshot, List<InvertedSource>> sourceSnapshotMap;

        private int[] cumulativeLineBreakCounts;
        private int[] cumulativeLengths;
        #endregion

        #region Construction
        public ProjectionSnapshot(ProjectionBuffer projectionBuffer, ITextVersion2 version, StringRebuilder content, IList<SnapshotSpan> sourceSpans)
            : base(version, content)
        {
            this.projectionBuffer = projectionBuffer;
            this.sourceSpans = new ReadOnlyCollection<SnapshotSpan>(sourceSpans);

            this.cumulativeLengths = new int[sourceSpans.Count + 1];
            this.cumulativeLineBreakCounts = new int[sourceSpans.Count + 1];

            this.sourceSnapshotMap = new Dictionary<ITextSnapshot, List<InvertedSource>>();
            for (int s = 0; s < sourceSpans.Count; ++s)
            {
                SnapshotSpan sourceSpan = sourceSpans[s];
                this.totalLength += sourceSpan.Length;

                this.cumulativeLengths[s + 1] = this.cumulativeLengths[s] + sourceSpan.Length;

                // Most source spans won't change when generating a new projection snapshot,
                // which means we should be able to reuse the line break count from the previous
                // projection snapshot.

                int lineBreakCount = sourceSpan.Snapshot.GetLineNumberFromPosition(sourceSpan.End) - sourceSpan.Snapshot.GetLineNumberFromPosition(sourceSpan.Start);

                // todo: incorrect when span ends with \r and following begins with \n
                this.totalLineCount += lineBreakCount;

                this.cumulativeLineBreakCounts[s + 1] = this.cumulativeLineBreakCounts[s] + lineBreakCount;

                ITextSnapshot snapshot = sourceSpan.Snapshot;
                List<InvertedSource> invertedSources;
                if (!this.sourceSnapshotMap.TryGetValue(snapshot, out invertedSources))
                {
                    invertedSources = new List<InvertedSource>();
                    this.sourceSnapshotMap.Add(snapshot, invertedSources);
                }
                invertedSources.Add(new InvertedSource(sourceSpan.Span, this.cumulativeLengths[s]));
            }

            // The SourceSnapshots property is heavily used, so calculate it once
            this.sourceSnapshots = new ReadOnlyCollection<ITextSnapshot>(new List<ITextSnapshot>(this.sourceSnapshotMap.Keys));

            // sort the per-buffer source span lists by position in source snapshot
            foreach (var v in this.sourceSnapshotMap.Values)
            {
                // sort by starting position. Spans can't overlap, but we do need null spans at a particular position
                // to precede non-null spans at that position, so if starting positions are equal, compare the ends.
                v.Sort((left, right) => (left.sourceSpan.Start == right.sourceSpan.Start 
                                            ? left.sourceSpan.End - right.sourceSpan.End 
                                            : left.sourceSpan.Start - right.sourceSpan.Start));
            }

            if (BufferGroup.Tracing)
            {
                Debug.WriteLine(LocalToString());
            }
            if (this.totalLength != version.Length)
            {
            	Debug.Fail(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                        "Projection Snapshot Inconsistency. Sum of spans: {0}, Previous + delta: {1}", this.totalLength, version.Length));
                throw new InvalidOperationException(Strings.InvalidLengthCalculation);
            }
            OverlapCheck();
        }

        private void OverlapCheck()
        {
            // try to diagnose problem with overlapping spans
            Dictionary<ITextSnapshot, List<Span>> groundSourceSpansMap = new Dictionary<ITextSnapshot, List<Span>>();
            MapDownToGround(this.sourceSpans, groundSourceSpansMap);

            foreach (KeyValuePair<ITextSnapshot, List<Span>> kvp in groundSourceSpansMap)
            {
                int sum = 0;
                foreach (Span s in kvp.Value)
                {
                    sum += s.Length;
                }
                NormalizedSpanCollection norma = new NormalizedSpanCollection(kvp.Value);
                int normaSum = 0;
                foreach (Span s in norma)
                {
                    normaSum += s.Length;
                }
                Debug.Assert(sum == normaSum);
                if (sum != normaSum)
                {
                    throw new InvalidOperationException(Strings.OverlappingSourceSpans);
                }
            }
        }

        private static void MapDownToGround(IList<SnapshotSpan> spans, Dictionary<ITextSnapshot, List<Span>> groundSourceSpansMap)
        {
            foreach (SnapshotSpan span in spans)
            {
                IProjectionSnapshot projSnap = span.Snapshot as IProjectionSnapshot;
                if (projSnap == null)
                {
                    List<Span> groundSpans;
                    if (!groundSourceSpansMap.TryGetValue(span.Snapshot, out groundSpans))
                    {
                        groundSpans = new List<Span>();
                        groundSourceSpansMap.Add(span.Snapshot, groundSpans);
                    }
                    groundSpans.Add(span);
                }
                else
                {
                    MapDownToGround(projSnap.MapToSourceSnapshots(span), groundSourceSpansMap);
                }
            }
        }
        #endregion

        #region Buffers and Spans
        public override IProjectionBufferBase TextBuffer
        {
            get { return this.projectionBuffer; }
        }

        protected override ITextBuffer TextBufferHelper
        {
            get { return this.projectionBuffer; }
        }

        public override int SpanCount
        {
            get { return this.sourceSpans.Count; }
        }

        public override ReadOnlyCollection<ITextSnapshot> SourceSnapshots
        {
            get { return this.sourceSnapshots; }
        }

        public override ITextSnapshot GetMatchingSnapshot(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }
            foreach (ITextSnapshot snappy in this.sourceSnapshotMap.Keys)
            {
                if (snappy.TextBuffer == textBuffer)
                {
                    return snappy;
                }
            }
            return null;
        }

        public override ITextSnapshot GetMatchingSnapshotInClosure(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }
            foreach (ITextSnapshot snappy in this.sourceSnapshotMap.Keys)
            {
                if (snappy.TextBuffer == textBuffer)
                {
                    return snappy;
                }
                IProjectionSnapshot2 projSnappy = snappy as IProjectionSnapshot2;
                if (projSnappy is IProjectionSnapshot2)
                {
                    ITextSnapshot maybe = projSnappy.GetMatchingSnapshotInClosure(textBuffer);
                    if (maybe != null)
                    {
                        return maybe;
                    }
                }
            }
            return null;
        }

        public override ITextSnapshot GetMatchingSnapshotInClosure(Predicate<ITextBuffer> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }
            foreach (ITextSnapshot snappy in this.sourceSnapshotMap.Keys)
            {
                if (match(snappy.TextBuffer))
                {
                    return snappy;
                }
                IProjectionSnapshot2 projSnappy = snappy as IProjectionSnapshot2;
                if (projSnappy is IProjectionSnapshot2)
                {
                    ITextSnapshot maybe = projSnappy.GetMatchingSnapshotInClosure(match);
                    if (maybe != null)
                    {
                        return maybe;
                    }
                }
            }
            return null;
        }

        public override ReadOnlyCollection<SnapshotSpan> GetSourceSpans()
        {
            return this.sourceSpans;
        }

        public override ReadOnlyCollection<SnapshotSpan> GetSourceSpans(int startSpanIndex, int count)
        {
            if (startSpanIndex < 0 || startSpanIndex > this.SpanCount)
            {
                throw new ArgumentOutOfRangeException(nameof(startSpanIndex));
            }
            if (count < 0 || startSpanIndex + count > this.SpanCount)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // better using iterator or explicit successor func eventually
            List<SnapshotSpan> resultSpans = new List<SnapshotSpan>(count);
            for (int i = 0; i < count; ++i)
            {
                resultSpans.Add(this.sourceSpans[startSpanIndex + i]);
            }
            return new ReadOnlyCollection<SnapshotSpan>(resultSpans);
        }

        internal SnapshotSpan GetSourceSpan(int position)
        {
            return this.sourceSpans[position];
        }
        #endregion

        #region Mapping
        public override ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshotsForRead(Span span)
        {
            if (span.End > this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            FrugalList<SnapshotSpan> mappedSpans = new FrugalList<SnapshotSpan>();

            if (span.Length == 0)
            {
                // First check for a degenerate snapshot having no source spans
                if (span.Start == 0 && this.sourceSpans.Count == 0)
                {
                    return new ReadOnlyCollection<SnapshotSpan>(mappedSpans);
                }

                // Zero-length spans are special in that they may map to more than one zero-length source span.
                // Defer to the point mapping implementation and then convert back to spans.
                ReadOnlyCollection<SnapshotPoint> points = MapInsertionPointToSourceSnapshots(span.Start, null);
                for (int p = 0; p < points.Count; ++p)
                {
                    SnapshotPoint point = points[p];
                    SnapshotSpan mappedSpan = new SnapshotSpan(point.Snapshot, point.Position, 0);
                    // avoid duplicates, caused by mapping the null span on a seam between source spans
                    // that come from the same source buffer and are adjacent in that source buffer
                    // Example: source spans are [0..10) and [10..20) from same source buffer, and we
                    // are requested to map the span at the seam, corresponding to [10..10).
                    if (mappedSpans.Count == 0 || mappedSpan != mappedSpans[mappedSpans.Count - 1])
                    {
                        mappedSpans.Add(mappedSpan);
                    }
                }
            }
            else
            {
                int rover = FindHighestSpanIndexOfPosition(span.Start);
                // sourceSpans[rover] contains span.Start

                SnapshotSpan sourceSpan = this.sourceSpans[rover];
                SnapshotPoint mappedStart = sourceSpan.Start + (span.Start - this.cumulativeLengths[rover]);
                int mappedLength = mappedStart.Position + span.Length < sourceSpan.End ? span.Length : sourceSpan.End.Position - mappedStart;
                mappedSpans.Add(new SnapshotSpan(mappedStart, mappedLength));

                // walk forward until we cover the entire span
                while (mappedLength < span.Length)
                {
                    sourceSpan = this.sourceSpans[++rover];
                    if (span.End >= this.cumulativeLengths[rover + 1])
                    {
                        mappedLength += sourceSpan.Length;
                        mappedSpans.Add(sourceSpan);
                    }
                    else
                    {
                        mappedLength += span.End - this.cumulativeLengths[rover];
                        mappedSpans.Add(new SnapshotSpan(sourceSpan.Snapshot, new Span(sourceSpan.Start, span.End - this.cumulativeLengths[rover])));
                    }
                }
            }

            return new ReadOnlyCollection<SnapshotSpan>(mappedSpans);
        }

        public override ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshots(Span span)
        {
            return MapToSourceSnapshotsForRead(span);
        }

        internal override ReadOnlyCollection<SnapshotSpan> MapReplacementSpanToSourceSnapshots(Span replacementSpan, ITextBuffer excludedBuffer)
        {
            // This is exactly like ordinary span mapping, except that empty source spans at the ends of the replacementSpan are included.

            FrugalList<SnapshotSpan> mappedSpans = new FrugalList<SnapshotSpan>();
            int rover = FindLowestSpanIndexOfPosition(replacementSpan.Start);
            int roverHi = FindHighestSpanIndexOfPosition(replacementSpan.End);
            // sourceSpans[rover] contains span.Start

            SnapshotSpan sourceSpan = this.sourceSpans[rover];
            {
                SnapshotPoint mappedStart = sourceSpan.Start + (replacementSpan.Start - this.cumulativeLengths[rover]);
                int mappedLength = mappedStart.Position + replacementSpan.Length < sourceSpan.End ? replacementSpan.Length : sourceSpan.End.Position - mappedStart;
                SnapshotSpan mappedSpan = new SnapshotSpan(mappedStart, mappedLength);
                if (mappedSpan.Length > 0 || mappedSpan.Snapshot.TextBuffer != excludedBuffer)
                {
                    mappedSpans.Add(new SnapshotSpan(mappedStart, mappedLength));
                }
            }

            // walk forward until we cover the entire span
            while (rover < roverHi)
            {
                rover++;
                sourceSpan = this.sourceSpans[rover];
                SnapshotSpan mappedSpan = replacementSpan.End >= this.cumulativeLengths[rover + 1]
                                            ? sourceSpan
                                            : new SnapshotSpan(sourceSpan.Snapshot, new Span(sourceSpan.Start, replacementSpan.End - this.cumulativeLengths[rover]));
                if (mappedSpan.Length > 0 || mappedSpan.Snapshot.TextBuffer != excludedBuffer)
                {
                    mappedSpans.Add(mappedSpan);
                }
            }

            Debug.Assert(replacementSpan.Length == mappedSpans.Sum((SnapshotSpan s) => s.Length), "Inconsistency in MapReplacementSpanToSourceSnapshots");

            return new ReadOnlyCollection<SnapshotSpan>(mappedSpans);
        }

        public override ReadOnlyCollection<Span> MapFromSourceSnapshot(SnapshotSpan sourceSpan)
        {
            List<InvertedSource> orderedSources;
            if (!this.sourceSnapshotMap.TryGetValue(sourceSpan.Snapshot, out orderedSources))
            {
                throw new ArgumentException("The span does not belong to a source snapshot of the projection snapshot");
            }

            Span spanToMap = sourceSpan.Span;

            // binary search for source span containing spanToMap.Start
            int lo = 0;
            int hi = orderedSources.Count - 1;
            int mid = 0;
            while (lo <= hi)
            {
                mid = (lo + hi) / 2;

                if (spanToMap.Start < orderedSources[mid].sourceSpan.Start)
                {
                    hi = mid - 1;
                }
                else if (spanToMap.Start > orderedSources[mid].sourceSpan.End)
                {
                    lo = mid + 1;
                }
                else
                {
                    break;
                    // orderedSources[mid].sourceSpan contains (or abuts at the end) sourceSpan.Start
                }
            }

            FrugalList<Span> result = new FrugalList<Span>();

            if (spanToMap.Start > orderedSources[mid].sourceSpan.End)
            {
                Debug.Assert(hi < lo, "Projection source span search exit invariant violated");
                // the binary search failed (hi and lo crossed) because spanToMap.Start did
                // not intersect any source span. However, it may be that some part of spanToMap will
                // intersect the next span, so we start our scan one span further along.

                // another way to think of this: if the binary search failed, mid will designate either the source span
                // to the left of the gap containing start or to the right of the gap containing start. This case
                // is where orderedSources[mid] lies to the left of the gap, and we don't want the loop below to blow out on 
                // the first iteration if spanToMap intersects orderedSources[mid+1].
                mid++;
            }

            for (int rover = mid; rover < orderedSources.Count; ++rover)
            {
                Span? s = spanToMap.Intersection(orderedSources[rover].sourceSpan);
                if (!s.HasValue)
                {
                    Debug.Assert(orderedSources[rover].sourceSpan.Start > spanToMap.End);
                    break;
                }
                if (s.Value.Length > 0 || spanToMap.Length == 0)
                {
                    result.Add(new Span(orderedSources[rover].projectedPosition + (s.Value.Start - orderedSources[rover].sourceSpan.Start), s.Value.Length));
                }
            }

            return new ReadOnlyCollection<Span>(result);
        }

        public override SnapshotPoint MapToSourceSnapshot(int position)
        {
            if (position < 0 || position > this.totalLength)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }
            ReadOnlyCollection<SnapshotPoint> points = this.MapInsertionPointToSourceSnapshots(position, this.projectionBuffer.literalBuffer);  // should this be conditional on writable literal buffer?
            if (points.Count == 1)
            {
                return points[0];
            }
            else if (this.projectionBuffer.resolver == null)
            {
                return points[points.Count - 1];
            }
            else
            {
                return points[this.projectionBuffer.resolver.GetTypicalInsertionPosition(new SnapshotPoint(this, position), points)];
            }
        }

        public override SnapshotPoint MapToSourceSnapshot(int position, PositionAffinity affinity)
        {
            if (position < 0 || position > this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }
            if (affinity < PositionAffinity.Predecessor || affinity > PositionAffinity.Successor)
            {
                throw new ArgumentOutOfRangeException(nameof(affinity));
            }

            int rover = affinity == PositionAffinity.Predecessor ? FindLowestSpanIndexOfPosition(position)
                                                                 : FindHighestSpanIndexOfPosition(position);
            if (rover < 0)
            {
                Debug.Assert(this.sourceSpans.Count == 0);
                throw new InvalidOperationException();
            }

            SnapshotSpan roverSpan = this.sourceSpans[rover];
            return roverSpan.Start + (position - cumulativeLengths[rover]);
        }

        public override SnapshotPoint? MapFromSourceSnapshot(SnapshotPoint sourcePoint, PositionAffinity affinity)
        {
            if (affinity < PositionAffinity.Predecessor || affinity > PositionAffinity.Successor)
            {
                throw new ArgumentOutOfRangeException(nameof(affinity));
            }

            List<InvertedSource> orderedSources;
            if (!this.sourceSnapshotMap.TryGetValue(sourcePoint.Snapshot, out orderedSources))
            {
                throw new ArgumentException("The point does not belong to a source snapshot of the projection snapshot");
            }

            int position = sourcePoint.Position;
            SnapshotPoint? candidatePoint = null;

            // binary search for source span containing pointToMap
            int lo = 0;
            int hi = orderedSources.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                Span sourceSpan = orderedSources[mid].sourceSpan;

                if (position < sourceSpan.Start)
                {
                    hi = mid - 1;
                }
                else if (position > sourceSpan.End)
                {
                    lo = mid + 1;
                }
                else
                {
                    candidatePoint = new SnapshotPoint(this, orderedSources[mid].projectedPosition + sourcePoint.Position - sourceSpan.Start);

                    if ((position > sourceSpan.Start && position < sourceSpan.End) ||
                        (position == sourceSpan.Start && affinity == PositionAffinity.Successor) ||
                        (position == sourceSpan.End && affinity == PositionAffinity.Predecessor))
                    {
                        // unambiguous
                        return candidatePoint;
                    }

                    if (position == sourceSpan.Start)
                    {
                        hi = mid - 1;
                    }
                    else if (position == sourceSpan.End)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        Debug.Fail("Ambiguous point mapping unexpected condition");
                        break;  // we have a decent answer 
                    }
                }
            }

            return candidatePoint;
        }

        /// <summary>
        /// Map insertion point in projection buffer into set of insertion points in source buffers. The
        /// result will have only one element unless the insertion is at the boundary of source spans, in which
        /// case there can be two (or more if empty source spans appear at the insertion location).
        /// </summary>
        internal override ReadOnlyCollection<SnapshotPoint> MapInsertionPointToSourceSnapshots(int position, ITextBuffer excludedBuffer)
        {
            if (position < 0 || position > this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            int rover = FindLowestSpanIndexOfPosition(position);

            SnapshotSpan sourceSpan = this.sourceSpans[rover];
            if (position < cumulativeLengths[rover + 1])
            {
                // point is not on a seam
                FrugalList<SnapshotPoint> singleResult = new FrugalList<SnapshotPoint>();
                singleResult.Add(sourceSpan.Start + (position - this.cumulativeLengths[rover]));
                return new ReadOnlyCollection<SnapshotPoint>(singleResult);
            }
            else
            {
                // point is at the boundary of source spans (this includes being at the
                // very beginning or end of the buffer, but that will work out OK).
                var sourceInsertionPoints = new FrugalList<SnapshotPoint>();

                // include the end point of the source span on the left
                var firstSnapshotPoint = new SnapshotPoint(sourceSpan.Snapshot, sourceSpan.End);
                if (sourceSpan.Snapshot.TextBuffer != excludedBuffer)
                {
                    sourceInsertionPoints.Add(firstSnapshotPoint);
                }

                // include all consecutive source spans of zero length (typically there are none of these)
                while (++rover < this.sourceSpans.Count &&  this.cumulativeLengths[rover] == this.cumulativeLengths[rover + 1])
                {
                    sourceSpan = this.sourceSpans[rover];
                    if (sourceSpan.Snapshot.TextBuffer != excludedBuffer)
                    {
                        sourceInsertionPoints.Add(new SnapshotPoint(sourceSpan.Snapshot, sourceSpan.Start));
                    }
                }

                // include first nonzero length source span (if any)
                if (rover < this.sourceSpans.Count)
                {
                    sourceSpan = this.sourceSpans[rover];
                    if (sourceSpan.Snapshot.TextBuffer != excludedBuffer)
                    {
                        sourceInsertionPoints.Add(new SnapshotPoint(sourceSpan.Snapshot, sourceSpan.Start));
                    }
                }

                if (sourceInsertionPoints.Count == 0)
                {
                    // Where position falls in the seam between two (or more if they are 0 length) spans from excludedBuffer and there
                    // are no snapshot points from the "real" buffers. In this case, the best thing to do is simply return the span from
                    // the excluded buffer (which is consistent with our behavior when position falls inside the middle of an excluded span).
                    sourceInsertionPoints.Add(firstSnapshotPoint);
                }

                return new ReadOnlyCollection<SnapshotPoint>(sourceInsertionPoints);
            }
        }
        #endregion

        #region Search
        /// <summary>
        /// Finds the highest index of the source span that intersects <paramref name="position"/>. This means
        /// that if the position is on a seam, the span to the "right" of the seam will be returned, and if
        /// there is a sequence of empty spans at the position, the index of the successor of the last of them will be
        /// returned.
        /// </summary>
        internal int FindHighestSpanIndexOfPosition(int position)
        {
            int lo = 0;
            int hi = this.sourceSpans.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (position < this.cumulativeLengths[mid])
                {
                    hi = mid - 1;
                }
                else if (position >= this.cumulativeLengths[mid + 1])
                {
                    lo = mid + 1;
                }
                else
                {
                    // sourceSpans[mid] contains position
                    return mid;
                }
            }
            Debug.Assert(position == this.Length);
            return this.sourceSpans.Count - 1;
        }

        /// <summary>
        /// Finds the lowest index of the source span that intersects <paramref name="position"/>. This means
        /// that if the position is on a seam, the span to the "left" of the seam will be returned, and if
        /// there is a sequence of empty spans at the position, the index of the first of them will be
        /// returned.
        /// </summary>
        internal int FindLowestSpanIndexOfPosition(int position)
        {
            int lo = 0;
            int hi = this.sourceSpans.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (position < this.cumulativeLengths[mid] || (mid > 0 && position == this.cumulativeLengths[mid]))
                {
                    hi = mid - 1;
                }
                else if (position > this.cumulativeLengths[mid + 1])
                {
                    lo = mid + 1;
                }
                else
                {
                    // sourceSpans[mid] contains position (or it is at the end of the span)
                    return mid;
                }
            }
            Debug.Assert(position == this.Length);
            return this.sourceSpans.Count - 1;
        }

        internal int FindLowestSpanIndexOfLineNumber(int lineNumber)
        {
            int lo = 0;
            int hi = this.sourceSpans.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (lineNumber <= this.cumulativeLineBreakCounts[mid] && mid > 0)
                {
                    hi = mid - 1;
                }
                else if (lineNumber > this.cumulativeLineBreakCounts[mid + 1])
                {
                    lo = mid + 1;
                }
                else
                {
                    // sourceSpans[mid] contains position
                    return mid;
                }
            }
            Debug.Assert(lineNumber == this.LineCount - 1);
            return this.sourceSpans.Count - 1;
        }

        #endregion

        #region Diagnostic Support
        private string LocalToString()
        {
            // need a non-virtual form to call from the constructor
            System.Text.StringBuilder image = new System.Text.StringBuilder();
            image.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                               "Snapshot {0,10} V{1}\r\n", TextUtilities.GetTagOrContentType(this.projectionBuffer), this.version.VersionNumber);
            int cumulativeLength = 0;
            for (int s = 0; s < this.sourceSpans.Count; ++s)
            {
                SnapshotSpan sourceSpan = this.sourceSpans[s];
                image.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                                   "{0,12} {1,10} {2,4} {3,12} {4}\r\n",
                                   new Span(cumulativeLength, sourceSpan.Length),
                                   TextUtilities.GetTagOrContentType(sourceSpan.Snapshot.TextBuffer),
                                   "V" + sourceSpan.Snapshot.Version.VersionNumber.ToString(CultureInfo.InvariantCulture),
                                   sourceSpan.Span,
                                   TextUtilities.Escape(sourceSpan.GetText()));
                cumulativeLength += sourceSpan.Length;
            }
            return image.ToString();
        }

        public override string ToString()
        {
            return LocalToString();
        }
        #endregion
    }
}
