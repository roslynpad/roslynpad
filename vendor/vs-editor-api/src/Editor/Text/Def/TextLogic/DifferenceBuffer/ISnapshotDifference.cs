//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A set of differences between two <see cref="ITextSnapshot"/>s. These are created by
    /// an <see cref="IDifferenceBuffer"/>, and are valid to a specific set of snapshots for the
    /// <see cref="IDifferenceBuffer.LeftBuffer"/>/<see cref="IDifferenceBuffer.RightBuffer"/> and
    /// the <see cref="StringDifferenceOptions"/> and collections of <see cref="SnapshotLineTransform"/> 
    /// and <see cref="IgnoreDifferencePredicate"/> in place at that time.
    /// </summary>
    public interface ISnapshotDifference
    {
        /// <summary>
        /// The <see cref="IDifferenceBuffer"/> that generated this difference.
        /// </summary>
        /// <remarks>
        /// To determine if this difference is current, you can compare it against
        /// <see cref="IDifferenceBuffer.CurrentSnapshotDifference"/>.
        /// </remarks>
        IDifferenceBuffer DifferenceBuffer { get; }

        /// <summary>
        /// The snapshot of the left buffer used to compute this difference.
        /// </summary>
        ITextSnapshot LeftBufferSnapshot { get; }

        /// <summary>
        /// The snapshot of the right buffer used to compute this difference.
        /// </summary>
        ITextSnapshot RightBufferSnapshot { get; }

        /// <summary>
        /// The snapshot generated for the inline buffer for this difference.
        /// </summary>
        IProjectionSnapshot InlineBufferSnapshot { get; }

        /// <summary>
        /// The difference options that were used to generate this difference.
        /// </summary>
        StringDifferenceOptions DifferenceOptions { get; }

        /// <summary>
        /// The line transforms that were used to generate this difference.
        /// </summary>
        IEnumerable<SnapshotLineTransform> SnapshotLineTransforms { get; }

        /// <summary>
        /// The ignore difference predicates that were used to generate this difference.
        /// </summary>
        IEnumerable<IgnoreDifferencePredicate> IgnoreDifferencePredicates { get; }

        /// <summary>
        /// The differences for this snapshot.
        /// </summary>
        /// <remarks>
        /// To find word-level differences, use <see cref="IHierarchicalDifferenceCollection.HasContainedDifferences"/> 
        /// and <see cref="IHierarchicalDifferenceCollection.GetContainedDifferences"/>.
        /// </remarks>
        IHierarchicalDifferenceCollection LineDifferences { get; }

        /// <summary>
        /// The word and line difference spans as <see cref="ITrackingSpan"/>s against the left and right buffer.
        /// </summary>
        IDifferenceTrackingSpanCollection DifferenceSpans { get; }

        #region Convenience methods

        /// <summary>
        /// Map a point from either the left or right buffer to the inline snapshot.
        /// </summary>
        /// <param name="point">The point to map up.</param>
        /// <returns>A point in the <see cref="InlineBufferSnapshot"/>.</returns>
        /// <remarks>This is equivalent to calling MapToSnapshot(point, snapshot.InlineBufferSnapshot).</remarks>
        SnapshotPoint MapToInlineSnapshot(SnapshotPoint point);

        /// <summary>
        /// Find the match or difference that contains the specified point.
        /// </summary>
        /// <param name="point">Point for which to find the corresponding difference. This can be on the left, right or inline buffers.</param>
        /// <param name="match">Match containing the <paramref name="point"/> (will be null if <paramref name="point"/> lies in a difference).</param>
        /// <param name="difference">Difference containing the <paramref name="point"/> (will be null if <paramref name="point"/> lies in a match).</param>
        /// <returns>Index of the matching difference.</returns>
        /// <remarks>
        /// <para> If the <paramref name="point"/> is contained in a match, then it is the index of the following difference. If <paramref name="point"/> is contained in a match
        /// after the last difference, then index will be equal to the count of differences.</para></remarks>
        int FindMatchOrDifference(SnapshotPoint point, out Match match, out Difference difference);

        /// <summary>
        /// Translate the specified point to the corresponding snapshot associated with snapshot difference.
        /// </summary>
        /// <param name="point">SnapshotPoint to translate.</param>
        /// <returns><paramref name="point"/> translated from its snapshot to this.LeftBufferSnapshot, this.RightBufferSnapshot, or this.InlineBufferSnapshot.</returns>
        SnapshotPoint TranslateToSnapshot(SnapshotPoint point);

        /// <summary>
        /// Map the specified <see cref="SnapshotPoint"/> in the inline buffer to its corresponding location in the left or right snapshots.
        /// </summary>
        /// <param name="inlinePoint">Point to map.</param>
        /// <returns>Corresponding location on either the left or right buffers.</returns>
        /// <remarks>
        /// <para>Locations inside matching text will always map to the right buffer.</para>
        /// </remarks>
        SnapshotPoint MapToSourceSnapshot(SnapshotPoint inlinePoint);

        /// <summary>
        /// Map the specified <see cref="SnapshotPoint"/> to the specified <see cref="ITextSnapshot"/>.
        /// </summary>
        /// <param name="point">Point to map.</param>
        /// <param name="target">Target snapshot</param>
        /// <param name="mode">The mapping used when mapping between the left and right snapshots (or vs. versa) when <paramref name="point"/> lies inside a difference.</param>
        /// <remarks>
        /// <para>Mapping to the left or right buffers may be lossy. Points inside a difference will be mapped according to <paramref name="mode"/>.</para>
        /// <para>Mapping between the inline snapshot and the source snapshots or vs. versa will, with one exception, be invertable. The exception is that a point between the \r\n
        /// of a line break may be mapped to the end of the corresponding line.</para>
        /// </remarks>
        SnapshotPoint MapToSnapshot(SnapshotPoint point, ITextSnapshot target, DifferenceMappingMode mode = DifferenceMappingMode.LineColumn);

        /// <summary>
        /// Get the extent of the difference in the specified snapshot.
        /// </summary>
        /// <param name="difference"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        SnapshotSpan MapToSnapshot(Difference difference, ITextSnapshot target);

        #endregion
    }
}
