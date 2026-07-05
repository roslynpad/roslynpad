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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Threading;

    using Microsoft.VisualStudio.Text.Implementation;

    internal abstract class BaseProjectionSnapshot : BaseSnapshot, IProjectionSnapshot2
    {
        #region State and Construction
        protected int totalLength = 0;
        protected int totalLineCount = 1;

        protected BaseProjectionSnapshot(ITextVersion2 version, StringRebuilder builder)
          : base(version, builder)
        {
        }
        #endregion

        public new abstract IProjectionBufferBase TextBuffer { get; }

        public ReadOnlyCollection<SnapshotPoint> MapToSourceSnapshots(int position)
        {
            return MapInsertionPointToSourceSnapshots(position, null);
        }

        #region Abstract Members
        /// <summary>
        /// Given the position of a pure insertion (not a replacement), return the list of source points at which the inserted text
        /// can be placed. This list has length greater than one only when the insertion point is on the seam of two or more source
        /// spans.
        /// </summary>
        /// <param name="position">The position of the insertion into the projection buffer.</param>
        /// <param name="excludedBuffer">Buffer to be ignored by virtue of its being a readonly literal buffer.</param>
        internal abstract ReadOnlyCollection<SnapshotPoint> MapInsertionPointToSourceSnapshots(int position, ITextBuffer excludedBuffer);

        /// <summary>
        /// Given the span of text to be deleted in a Replace operation, return the list of source spans to which it maps. Include any
        /// zero-length spans either on the boundaries or in the middle of the replacement span; the idea is both map to the deleted
        /// text and return the list of positions across which the inserted text can be placed.
        /// </summary>
        /// <param name="replacementSpan">The span of text to be replaced.</param>
        /// <param name="excludedBuffer">Buffer to be ignored by virtue of its being a readonly literal buffer; only zero-length spans are possible
        /// in this buffer.</param>
        internal abstract ReadOnlyCollection<SnapshotSpan> MapReplacementSpanToSourceSnapshots(Span replacementSpan, ITextBuffer excludedBuffer);

        public abstract int SpanCount { get; }
        public abstract ReadOnlyCollection<ITextSnapshot> SourceSnapshots { get; }
        public abstract ITextSnapshot GetMatchingSnapshot(ITextBuffer textBuffer);
        public abstract ITextSnapshot GetMatchingSnapshotInClosure(ITextBuffer targetBuffer);
        public abstract ITextSnapshot GetMatchingSnapshotInClosure(Predicate<ITextBuffer> match);
        public abstract ReadOnlyCollection<SnapshotSpan> GetSourceSpans(int startSpanIndex, int count);
        public abstract ReadOnlyCollection<SnapshotSpan> GetSourceSpans();

        public abstract SnapshotPoint MapToSourceSnapshot(int position);
        public abstract SnapshotPoint MapToSourceSnapshot(int position, PositionAffinity affinity);
        public abstract SnapshotPoint? MapFromSourceSnapshot(SnapshotPoint point, PositionAffinity affinity);
        public abstract ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshots(Span span);
        public abstract ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshotsForRead(Span span);
        public abstract ReadOnlyCollection<Span> MapFromSourceSnapshot(SnapshotSpan span);
        #endregion
    }
}
