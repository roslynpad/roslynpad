//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// An immutable text snapshot that represents a state of an <see cref="IProjectionBuffer"/>.
    /// This snapshot contains projections of other text snapshots, described
    /// by a list of tracking spans from those buffers. Every modification of a projection buffer
    /// or one of its source buffers generates a new projection snapshot.
    /// </summary>
    public interface IProjectionSnapshot : ITextSnapshot
    {
        /// <summary>
        /// Gets the <see cref="IProjectionBufferBase"/> of which this is a snapshot.
        /// </summary>
        /// <remarks>
        /// This property always returns the same projection buffer, but the projection buffer is not itself immutable.
        /// </remarks>
        new IProjectionBufferBase TextBuffer { get; }

        /// <summary>
        /// Gets the number of source spans in the projection snapshot.
        /// </summary>
        int SpanCount { get; }

        /// <summary>
        /// Gets the set of one or more text snapshots that contribute source spans to this projection snapshot. 
        /// The ordering of the list is arbitrary. It does not contain duplicates.
        /// </summary>
        ReadOnlyCollection<ITextSnapshot> SourceSnapshots { get; }

        /// <summary>
        /// Gets the snapshot of the specified text buffer that corresponds to this snapshot.
        /// </summary>
        /// <param name="textBuffer"></param>
        /// <returns>The snapshot of the text buffer. Returns null if <paramref name="textBuffer"/> is not a text buffer of this projection buffer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer"/> is null.</exception>
        ITextSnapshot GetMatchingSnapshot(ITextBuffer textBuffer);

        /// <summary>
        /// Gets a read-only collection of source snapshot spans starting at the specified span index.
        /// The <paramref name="startSpanIndex"/> is an index into the collection of source spans, not into the characters
        /// in the text buffer.
        /// </summary>
        /// <param name="startSpanIndex">The position at which to start getting snapshot spans.</param>
        /// <param name="count">The number of spans to get.</param>
        /// <returns>A read-only collection of <see cref="SnapshotSpan"/> objects that are sources of the projection snapshot.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startSpanIndex"/> is less than zero or greater than SpanCount.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero or <paramref name="count"/> plus <paramref name="startSpanIndex"/> 
        /// is greater than SpanCount.</exception>
        ReadOnlyCollection<SnapshotSpan> GetSourceSpans(int startSpanIndex, int count);

        /// <summary>
        /// Gets all the source spans for the projection snapshot.
        /// </summary>
        /// <returns>A read-only collection of source spans of the projection snapshot, listed in the order they have in the projection snapshot.
        /// The collection may be empty.</returns>
        ReadOnlyCollection<SnapshotSpan> GetSourceSpans();

        /// <summary>
        /// Maps a position in the projection snapshot to the corresponding position in a source snapshot. 
        /// </summary>
        /// <param name="position">The position in the projection snapshot .</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous (the position lies on a source span seam), this parameter affects the mapping as follows:
        /// if <paramref name="affinity"/> is <see cref="PositionAffinity.Predecessor"/>, the mapping targets 
        /// the position immediately after the preceding character in the projection buffer; if <paramref name="affinity"/> is 
        /// <see cref="PositionAffinity.Successor"/>, the mapping targets the position immediately before the following character
        /// in the projection buffer. This parameter has no effect if the mapping is unambiguous.</param>
        /// <returns>A snapshot point in one of the source snapshots.</returns>
        /// <remarks>
        /// In general, a source span seam occurs at the end of a source span of nonzero length
        /// and the beginning of a source span of nonzero length, and
        /// coincides with zero or more source spans of zero length.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than or equal to the length of the snapshot.</exception>
        /// <exception cref="InvalidOperationException">The projection snapshot has no source spans.</exception>
        SnapshotPoint MapToSourceSnapshot(int position, PositionAffinity affinity);

        /// <summary>
        /// Maps a position in the projection snapshot to the corresponding position in one or more source snapshots.
        /// </summary>
        /// <param name="position">The position in the projection snapshot.</param>
        /// <returns>A read-only collection of snapshot points to which the position maps. This collection contains one snapshot point unless the position lies
        /// on a source span seam, in which case it can contain two or more points.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than or equal to the length of the snapshot.</exception>
        /// <remarks>
        /// In general, a source span seam occurs at the end of a source span of nonzero length
        /// and the beginning of a source span of nonzero length, and
        /// coincides with zero or more source spans of zero length. Every span on a seam
        /// has a point in the result collection.
        /// </remarks>
        ReadOnlyCollection<SnapshotPoint> MapToSourceSnapshots(int position);

        /// <summary>
        /// Maps a position in the projection snapshot to the corresponding position in a source snapshot. If the mapping
        /// is ambiguous (occurs on a source span seam), see <see cref="IProjectionEditResolver.GetTypicalInsertionPosition"/>
        /// to choose a source buffer.
        /// </summary>
        /// <param name="position">The position in the projection snapshot.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than or equal to the length of the snapshot.</exception>
        SnapshotPoint MapToSourceSnapshot(int position);

        /// <summary>
        /// Maps from a snapshot point in one of the source snapshots to the corresponding position in the projection snapshot.
        /// </summary>
        /// <param name="point">The snapshot point in a source buffer.</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous (the position lies between two source spans), this parameter affects the mapping as follows:
        /// if <paramref name="affinity"/> is <see cref="PositionAffinity.Predecessor"/>, the mapping targets 
        /// the position immediately after the preceding character in the projection buffer; if <paramref name="affinity"/> is 
        /// <see cref="PositionAffinity.Successor"/>, the mapping targets the position immediately before the following character
        /// in the projection buffer. This parameter has no effect if the mapping is unambiguous.</param>
        /// <returns>A position in the projection snapshot, or null if the source point does not correspond
        /// to text belonging to a span that is a member of the projection snapshot.</returns>
        /// <remarks>
        /// In general, a source span seam occurs at the end of a source span of nonzero length
        /// and the beginning of a source span of nonzero length, and
        /// coincides with zero or more source spans of zero length. Every span on a seam
        /// has a point in the result collection.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="point"/> does not belong to a source snapshot of this projection snapshot.</exception>
        SnapshotPoint? MapFromSourceSnapshot(SnapshotPoint point, PositionAffinity affinity);

        /// <summary>
        /// Maps a span of the current projection snapshot to a list of snapshot spans belonging to source
        /// snapshots. The resulting spans will be ordered by the order of their appearance in the projection.
        /// </summary>
        /// <param name="span">The span in the projection snapshot.</param>
        /// <returns>A non-empty list of snapshot spans.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/> is not valid for this buffer.</exception>
        /// <remarks>If a null span occurs on a source span seam, it may map to more than one null source span.</remarks>
        ReadOnlyCollection<SnapshotSpan> MapToSourceSnapshots(Span span);

        /// <summary>
        /// Maps a snapshot span of a source buffer to a list of spans of the projection snapshot. 
        /// The resulting ordered list may be empty, contain a single element, or contain multiple elements.
        /// </summary>
        /// <param name="span">The snapshot span in a source buffer to map.</param>
        /// <returns>A non-null list of spans. The list will be empty if none of the positions in <paramref name="span"/> are projected by a source span
        /// of the projection snapshot. This list is <b>not</b> normalized; the spans will be ordered by their original position in the
        /// source snapshot, not their position in the projection snapshot. Adjacent spans are not coalesced.</returns>
        /// <exception cref="ArgumentException"><paramref name="span"/> does not belong to a source buffer of this projection buffer.</exception>
        ReadOnlyCollection<Span> MapFromSourceSnapshot(SnapshotSpan span);
    }
}
