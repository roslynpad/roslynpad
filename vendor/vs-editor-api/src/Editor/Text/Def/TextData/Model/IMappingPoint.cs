//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using Microsoft.VisualStudio.Text.Projection;

    /// <summary>
    /// A position in a <see cref="ITextBuffer"/> that can be mapped within a <see cref="Morgania.Text.Projection.IBufferGraph"/>.
    /// </summary>
    public interface IMappingPoint
    {
        /// <summary>
        /// Maps the point to a particular <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="targetBuffer">The <see cref="ITextBuffer"/> to which to map the point.</param>
        /// <param name="affinity">If the mapping is ambiguous (the position lies on a source span seam), this parameter affects the mapping as follows:
        /// if <paramref name="affinity"/> is <see cref="PositionAffinity.Predecessor"/>, the mapping targets 
        /// the position immediately after the preceding character in the anchor buffer; if <paramref name="affinity"/> is 
        /// <see cref="PositionAffinity.Successor"/>, the mapping targets the position immediately before the following character
        /// in the anchor buffer. This parameter has no effect if the mapping is unambiguous.</param>
        /// <returns>A <see cref="SnapshotPoint"/> in the targeted buffer or null if the point and affinity do not appear in that buffer.</returns>
        /// <remarks>
        /// In general, a source span seam occurs at the end of a source span of nonzero length
        /// and the beginning of a source span of nonzero length, and
        /// coincides with zero or more source spans of zero length. Every span on a seam
        /// has a point in the result collection.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="targetBuffer"/> is null.</exception>
        SnapshotPoint? GetPoint(ITextBuffer targetBuffer, PositionAffinity affinity);
        
        /// <summary>
        /// Maps the point to a particular <see cref="ITextSnapshot"/>.
        /// </summary>
        /// <param name="targetSnapshot">The <see cref="ITextSnapshot"/> to which to map the point.</param>
        /// <param name="affinity">If the mapping is ambiguous (the position lies on a source span seam), this parameter affects the mapping as follows:
        /// if <paramref name="affinity"/> is <see cref="PositionAffinity.Predecessor"/>, the mapping targets 
        /// the position immediately after the preceding character in the anchor buffer; if <paramref name="affinity"/> is 
        /// <see cref="PositionAffinity.Successor"/>, the mapping targets the position immediately before the following character
        /// in the anchor buffer. This parameter has no effect if the mapping is unambiguous.</param>
        /// <returns>A <see cref="SnapshotPoint"/> in the targeted buffer or null if the point and affinity do not appear in that buffer.</returns>
        /// <remarks>
        /// In general, a source span seam occurs at the end of a source span of nonzero length
        /// and the beginning of a source span of nonzero length, and
        /// coincides with zero or more source spans of zero length. Every span on a seam
        /// has a point in the result collection.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="targetSnapshot"/> is null.</exception>
        SnapshotPoint? GetPoint(ITextSnapshot targetSnapshot, PositionAffinity affinity);
    
        /// <summary>
        /// Maps the point to a matching <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="match">The predicate used to match the <see cref="ITextBuffer"/>.</param>
        /// <param name="affinity">If the mapping is ambiguous (the position lies on a source span seam), this parameter affects the mapping as follows:
        /// if <paramref name="affinity"/> is <see cref="PositionAffinity.Predecessor"/>, the mapping targets 
        /// the position immediately after the preceding character in the anchor buffer; if <paramref name="affinity"/> is 
        /// <see cref="PositionAffinity.Successor"/>, the mapping targets the position immediately before the following character
        /// in the anchor buffer. This parameter has no effect if the mapping is unambiguous.</param>
        /// <returns>A <see cref="SnapshotPoint"/> in the matching buffer, or null if the point and affinity do not appear in that buffer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
        /// <remarks><paramref name="match"/> will be called as text buffers in the buffer graph are encountered, until a match is found. 
        /// This selects the buffer of interest and <paramref name="match"/> is not called again. 
        /// If no match is found with any of the buffers, the result is null.</remarks>
        SnapshotPoint? GetPoint(Predicate<ITextBuffer> match, PositionAffinity affinity);

        /// <summary>
        /// Maps the point to an insertion point in a matching <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="match">The predicate used to match the <see cref="ITextBuffer"/>.</param>
        /// <returns>A <see cref="SnapshotPoint"/> in the matching buffer or null if the point does not appear in that buffer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
        /// <remarks>In the usual case, this is a straightforward computation that maps through projection buffers, subject to
        /// caller approval using <paramref name="match"/>. If there is ambiguity in a projection mapping, the
        /// <see cref="IProjectionEditResolver.GetTypicalInsertionPosition"/> method for the relevant projection buffer will be consulted.
        /// <paramref name="match"/> will be called as text buffers in the buffer graph are encountered, until a match is found. 
        /// This selects the buffer of interest and the predicate will not be called again. 
        /// If no match is found with any of encountered buffers, the result will be null.</remarks>
        SnapshotPoint? GetInsertionPoint(Predicate<ITextBuffer> match);

        /// <summary>
        /// The <see cref="ITextBuffer"/> from which this point was created.
        /// </summary>
        ITextBuffer AnchorBuffer { get; }

        /// <summary>
        /// The <see cref="IBufferGraph"/> that this point uses to perform the mapping.
        /// </summary>
        IBufferGraph BufferGraph { get; }
    }
}