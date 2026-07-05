//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Represents a graph of <see cref="ITextBuffer"/> objects. The 
    /// top level text buffer might or might not be a <see cref="IProjectionBuffer"/>.
    /// </summary>
    public interface IBufferGraph
    {
        /// <summary>
        /// Gets the top text buffer in the buffer graph.
        /// </summary>
        ITextBuffer TopBuffer { get; }

        /// <summary>
        /// Finds all the <see cref="ITextBuffer"/> objects in the graph that match the specified predicate.
        /// </summary>
        /// <param name="match">The predicate used for matching.</param>
        /// <returns>A non-null but possibly empty collection of <see cref="ITextBuffer"/> objects.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
        Collection<ITextBuffer> GetTextBuffers(Predicate<ITextBuffer> match);

        /// <summary>
        /// Creates a new <see cref="IMappingPoint"/> with the specified snapshot point and tracking mode.
        /// </summary>
        /// <param name="point">A <see cref="SnapshotPoint"/> in one of the buffers of the graph.</param>
        /// <param name="trackingMode">How to track the point.</param>
        /// <returns>A <see cref="IMappingPoint"/> that can track within its buffer and map within the graph.</returns>
        IMappingPoint CreateMappingPoint(SnapshotPoint point, PointTrackingMode trackingMode);

        /// <summary>
        /// Initializes a new instance of a  <see cref="IMappingSpan"/>.
        /// </summary>
        /// <param name="span">A <see cref="SnapshotSpan"/> in one of the buffers of the graph.</param>
        /// <param name="trackingMode">How to track the span.</param>
        /// <returns>A <see cref="IMappingSpan"/> that can track within its buffer and map within the graph.</returns>
        IMappingSpan CreateMappingSpan(SnapshotSpan span, SpanTrackingMode trackingMode);

        /// <summary>
        /// Maps a position in the graph to the corresponding position in a buffer lower in the graph. Source buffers are considered to be lower than 
        /// the projection buffers that consume them.
        /// </summary>
        /// <param name="position">The position in a buffer in the graph.</param>
        /// <param name="trackingMode">How <paramref name="position"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="targetBuffer">The buffer to which to map the <paramref name="position"/>.</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous (the position is on a source span seam), determines
        /// whether the mapping should target the position immediately after the preceding
        /// character or immediately before the following character in the top buffer.
        /// This setting  has no effect if the mapping is unambiguous.</param>
        /// <returns>A point in a snapshot of the target buffer, or null if <paramref name="position"/> is not in this graph or does not map to 
        /// the target buffer with the given affinity.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="position"/>.Snapshot or <paramref name="targetBuffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="PointTrackingMode"/>, or
        /// <paramref name="affinity"/> is not a valid <see cref="PositionAffinity"/>.</exception>
        SnapshotPoint? MapDownToBuffer(SnapshotPoint position, PointTrackingMode trackingMode, ITextBuffer targetBuffer, PositionAffinity affinity);

        /// <summary>
        /// Maps a position in the graph to the corresponding position in a snapshot lower in the graph. Source buffers are considered to be lower than 
        /// the projection buffers that consume them.
        /// </summary>
        /// <param name="position">The position in a buffer in the graph.</param>
        /// <param name="trackingMode">How <paramref name="position"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="targetSnapshot">The buffer to which to map the <paramref name="position"/>.</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous (the position is on a source span seam), determines
        /// whether the mapping should target the position immediately after the preceding
        /// character or immediately before the following character in the top buffer.
        /// This setting has no effect if the mapping is unambiguous.</param>
        /// <returns>A point in a snapshot of the target buffer, or null if <paramref name="position"/> is not in this graph or does not map to the
        /// target buffer with the given affinity.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="position"/>.Snapshot or <paramref name="targetSnapshot"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="PointTrackingMode"/>, or
        /// <paramref name="affinity"/> is not a valid <see cref="PositionAffinity"/>.</exception>
        SnapshotPoint? MapDownToSnapshot(SnapshotPoint position, PointTrackingMode trackingMode, ITextSnapshot targetSnapshot, PositionAffinity affinity);

        /// <summary>
        /// Maps a position in the graph to a position in a matching buffer that is lower in the graph. Source buffers are 
        /// considered to be lower than the projection buffers that consume them.
        /// </summary>
        /// <param name="position">The position in a buffer in the graph.</param>
        /// <param name="trackingMode">How <paramref name="position"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="match">The predicate that identifies the target buffer.</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous (the position is on a source span seam), determines
        /// whether the mapping should target the position immediately after the preceding
        /// character or immediately before the following character in the top buffer.
        /// This setting has no effect if the mapping is unambiguous.</param>
        /// <returns>A point in a snapshot of the target buffer, or null if <paramref name="position"/> does not map down to any buffer 
        /// selected by <paramref name="match"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="position"/>.Snapshot or <paramref name="match"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="PointTrackingMode"/>, or
        /// <paramref name="affinity"/> is not a valid <see cref="PositionAffinity"/>.</exception>
        /// <remarks>The <paramref name="match"/> predicate is called on each text buffer in the buffer graph until it
        /// returns <c>true</c>. The predicate will not be called again.</remarks>
        SnapshotPoint? MapDownToFirstMatch(SnapshotPoint position, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match, PositionAffinity affinity);

        /// <summary>
        /// Maps a position in some buffer in the graph to a position in a matching buffer that is lower in the graph and to which an
        /// insertion would be routed. Source buffers are considered to be lower than the projection buffers that consume them.
        /// </summary>
        /// <param name="position">the position in a buffer in the graph.</param>
        /// <param name="trackingMode">How <paramref name="position"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="match">The predicate that identifies the target buffer.</param>
        /// <returns>A point in a snapshot of some source buffer, or null if <paramref name="position"/> is not in this graph or  does not
        /// map down to any buffer selected by <paramref name="match"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="position"/>.Snapshot or <paramref name="match"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="PointTrackingMode"/>.</exception>
        SnapshotPoint? MapDownToInsertionPoint(SnapshotPoint position, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match);

        /// <summary>
        /// Maps a snapshot span in some buffer in the graph to a sequence of zero or more spans in a buffer that is lower in the graph.
        /// Source buffers are considered to be lower than the projection buffers that consume them.
        /// </summary>
        /// <param name="span">The span that is to be mapped.</param>
        /// <param name="trackingMode">How <paramref name="span"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="targetBuffer">The buffer to which to map the span.</param>
        /// <returns>A collection of zero or more snapshot spans in the target buffer to which the span maps.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/>.Snapshot or <paramref name="targetBuffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="SpanTrackingMode"/>.</exception>
        NormalizedSnapshotSpanCollection MapDownToBuffer(SnapshotSpan span, SpanTrackingMode trackingMode, ITextBuffer targetBuffer);

        /// <summary>
        /// Maps a snapshot span in some buffer in the graph to a sequence of zero or more spans in a buffer that is lower in the graph.
        /// Source buffers are considered to be lower than the projection buffers that consume them.
        /// </summary>
        /// <param name="span">The span that is to be mapped.</param>
        /// <param name="trackingMode">How <paramref name="span"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="targetSnapshot">The buffer to which to map the span.</param>
        /// <returns>A collection of zero or more snapshot spans in the target buffer to which the span maps.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/>.Snapshot or <paramref name="targetSnapshot"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="SpanTrackingMode"/>.</exception>
        NormalizedSnapshotSpanCollection MapDownToSnapshot(SnapshotSpan span, SpanTrackingMode trackingMode, ITextSnapshot targetSnapshot);

        /// <summary>
        /// Maps a snapshot span in some buffer in the graph to a sequence of zero or more spans in some source snapshot selected by a predicate.
        /// </summary>
        /// <param name="span">The span that is to be mapped.</param>
        /// <param name="trackingMode">How <paramref name="span"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="match">The predicate that identifies the target buffer.</param>
        /// <returns>A collection of zero or more snapshot spans in the target buffer to which the topSpan maps.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/>.Snapshot or <paramref name="match"/>  is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="SpanTrackingMode"/>.</exception>
        /// <remarks><paramref name="match"/> is called on each text buffer in the buffer graph until it
        /// returns <c>true</c>. The predicate will not be called again.</remarks>
        NormalizedSnapshotSpanCollection MapDownToFirstMatch(SnapshotSpan span, SpanTrackingMode trackingMode, Predicate<ITextSnapshot> match);

        /// <summary>
        /// Maps a position in the current snapshot of some buffer that is a member of the buffer graph to a snapshot of some buffer.
        /// </summary>
        /// <param name="point">A point in some buffer in the <see cref="IBufferGraph"/>.</param>
        /// <param name="trackingMode">How <paramref name="point"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous (the position is on a source span seam), determines
        /// whether the mapping should target the position immediately after the preceding
        /// character or immediately before the following character in the top buffer.
        /// This setting has no effect if the mapping is unambiguous.</param>
        /// <param name="targetBuffer">The buffer to which to map.</param>
        /// <returns>The corresponding position in a snapshot of the target buffer, or null if the position does not map to the target buffer 
        /// using this graph.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="point"/>.Snapshot is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="PointTrackingMode"/>, or
        /// <paramref name="affinity"/> is not a valid <see cref="PositionAffinity"/>.</exception>
        SnapshotPoint? MapUpToBuffer(SnapshotPoint point, PointTrackingMode trackingMode, PositionAffinity affinity, ITextBuffer targetBuffer);

        /// <summary>
        /// Maps a position in the current snapshot of some buffer that is a member of the buffer graph to specified snapshot.
        /// </summary>
        /// <param name="point">A point in some buffer in the <see cref="IBufferGraph"/>.</param>
        /// <param name="trackingMode">How <paramref name="point"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous (the position is on a source span seam), determines
        /// whether the mapping should target the position immediately after the preceding
        /// character or immediately before the following character in the top buffer.
        /// This setting has no effect if the mapping is unambiguous.</param>
        /// <param name="targetSnapshot">The snapshot to which to map.</param>
        /// <returns>The corresponding position in <paramref name="targetSnapshot"/>, or null if the position does not map to <paramref name="targetSnapshot"/> 
        /// using this graph.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="point"/>.Snapshot is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="PointTrackingMode"/>, or
        /// <paramref name="affinity"/> is not a valid <see cref="PositionAffinity"/>.</exception>
        SnapshotPoint? MapUpToSnapshot(SnapshotPoint point, PointTrackingMode trackingMode, PositionAffinity affinity, ITextSnapshot targetSnapshot);

        /// <summary>
        /// Maps a position in the current snapshot of some buffer that is a member of the buffer graph to a snapshot of some buffer
        /// that is selected by a predicate.
        /// </summary>
        /// <param name="point">A point in some buffer in the <see cref="IBufferGraph"/>.</param>
        /// <param name="trackingMode">How <paramref name="point"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="affinity">
        /// If the mapping is ambiguous (the position is on a source span seam), determines
        /// whether the mapping should target the position immediately after the preceding
        /// character or immediately before the following character in the top buffer.
        /// This setting has no effect if the mapping is unambiguous.</param>
        /// <param name="match">The predicate that identifies the target buffer.</param>
        /// <remarks><paramref name="match"/> is called for each text buffer in the buffer graph until it
        /// returns <c>true</c>. The predicate will not be called again.</remarks>
        /// <returns>The corresponding position in a snapshot of the matching buffer, or null if does not map to the matching buffer using
        /// this graph.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="point"/>.Snapshot or <paramref name="match"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="PointTrackingMode"/>, or
        /// <paramref name="affinity"/> is not a valid <see cref="PositionAffinity"/>.</exception>
        SnapshotPoint? MapUpToFirstMatch(SnapshotPoint point, PointTrackingMode trackingMode, Predicate<ITextSnapshot> match, PositionAffinity affinity);

        /// <summary>
        /// Maps a span in the current snapshot of some buffer that is a member of the buffer graph to a sequence of spans in a snapshot of 
        /// a designated buffer.
        /// </summary>
        /// <param name="span">A span in some buffer in the <see cref="IBufferGraph"/>.</param>
        /// <param name="trackingMode">How <paramref name="span"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="targetBuffer">The buffer to which to map.</param>
        /// <returns>A collection of zero or more snapshot spans in <paramref name="targetBuffer"/> to which the span maps using this graph.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/>.Snapshot is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="SpanTrackingMode"/>.</exception>
        NormalizedSnapshotSpanCollection MapUpToBuffer(SnapshotSpan span, SpanTrackingMode trackingMode, ITextBuffer targetBuffer);

        /// <summary>
        /// Maps a span in the current snapshot of some buffer that is a member of the buffer graph to a sequence of spans in a snapshot of 
        /// a designated buffer.
        /// </summary>
        /// <param name="span">A span in some buffer in the <see cref="IBufferGraph"/>.</param>
        /// <param name="trackingMode">How <paramref name="span"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="targetSnapshot">The snapshot to which to map.</param>
        /// <returns>A collection of zero or more snapshot spans in <paramref name="targetSnapshot"/> to which the span maps using this graph.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/>.Snapshot is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="SpanTrackingMode"/>.</exception>
        NormalizedSnapshotSpanCollection MapUpToSnapshot(SnapshotSpan span, SpanTrackingMode trackingMode, ITextSnapshot targetSnapshot);

        /// <summary>
        /// Maps a span in the current snapshot of some buffer that is a member of the buffer graph up to a sequence of spans in a snapshot of 
        /// some buffer that is selected by a predicate.
        /// </summary>
        /// <param name="span">A span in some buffer in the IBufferGraph.</param>
        /// <param name="trackingMode">How <paramref name="span"/> is tracked to the current snapshot if necessary.</param>
        /// <param name="match">The predicate that identifies the target buffer.</param>
        /// <returns>A collection of zero or more snapshot spans in the buffer selected by <paramref name="match"/>.</returns>
        /// <remarks><paramref name="match"/> is called on each text buffer in the graph until it
        /// returns <c>true</c>. The predicate will not be called again.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="span"/>.Snapshot or <paramref name="match"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="trackingMode"/> is not a valid <see cref="SpanTrackingMode"/>.</exception>
        NormalizedSnapshotSpanCollection MapUpToFirstMatch(SnapshotSpan span, SpanTrackingMode trackingMode, Predicate<ITextSnapshot> match);

        /// <summary>
        /// Occurs when the set of <see cref="ITextBuffer"/> objects in the buffer graph changes.
        /// </summary>
        event EventHandler<GraphBuffersChangedEventArgs> GraphBuffersChanged;

        /// <summary>
        /// Occurs when the <see cref="Morgania.Utilities.IContentType"/> of any <see cref="ITextBuffer"/> in the buffer graph changes.
        /// </summary>
        event EventHandler<GraphBufferContentTypeChangedEventArgs> GraphBufferContentTypeChanged;
    }
}
