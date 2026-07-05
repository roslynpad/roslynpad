//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Allows the creator of a projection buffer to control behavior of certain edits to the buffer.
    /// </summary>
    public interface IProjectionEditResolver
    {
        /// <summary>
        /// When text is inserted into the projection buffer at <paramref name="projectionInsertionPoint"/>, determine how many characters
        /// of the <paramref name="insertionText"/> are to be inserted into the source buffer at each source insertion point.
        /// If length of the <paramref name="sourceInsertionPoints"/> is greater than two, all but the first and last snapshot point will denote
        /// the boundary of an empty source span.
        /// </summary>
        /// <remarks>
        /// This call is made while an edit is in progress, so any attempt to change the projection buffer or its sources during
        /// this call will fail.
        /// </remarks>
        /// <param name="projectionInsertionPoint">The insertion point in the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="sourceInsertionPoints">The list of insertion points in the source buffers (of length two or more).</param>
        /// <param name="insertionText">The text to be split between the insertion points.</param>
        /// <param name="insertionSizes">Filled in by the callee; the number of characters in the <paramref name="insertionText"/> to be inserted into the corresponding source insertion point.</param>
        void FillInInsertionSizes(SnapshotPoint projectionInsertionPoint,
                                  ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints,
                                  string insertionText,
                                  IList<int> insertionSizes);

        /// <summary>
        /// When text at <paramref name="projectionReplacementSpan"/> is replaced in a projection buffer, determine how many characters
        /// of the <paramref name="insertionText"/> are to be inserted into the source buffer at each source insertion point (which are
        /// the Start points of the <paramref name="sourceReplacementSpans"/>).
        /// </summary>
        /// <remarks>
        /// This call is made while an edit is in progress, so any attempt to change the projection buffer or its sources during
        /// this call will fail.
        /// </remarks>
        /// <param name="projectionReplacementSpan">The span of text that is to be replaced in the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="sourceReplacementSpans">The spans of text that are to be replaced in the source buffers (of length two or more).</param>
        /// <param name="insertionText">The text to be split among the replacement spans.</param>
        /// <param name="insertionSizes">Filled in by the callee; the number of characters in the <paramref name="insertionText"/> to 
        /// be inserted into the corresponding source replacement span.</param>
        void FillInReplacementSizes(SnapshotSpan projectionReplacementSpan,
                                    ReadOnlyCollection<SnapshotSpan> sourceReplacementSpans,
                                    string insertionText,
                                    IList<int> insertionSizes);

        /// <summary>
        /// When a position in the projection buffer lies on a source buffer seam, determine which source insertion
        /// point would receive a typical insertion.
        /// </summary>
        /// <param name="projectionInsertionPoint">The insertion point in the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="sourceInsertionPoints">The list of insertion points in the source buffers (of length two or more).</param>
        /// <returns>An integer between 0 and <paramref name="sourceInsertionPoints"/>.Length - 1.</returns>
        int GetTypicalInsertionPosition(SnapshotPoint projectionInsertionPoint,
                                        ReadOnlyCollection<SnapshotPoint> sourceInsertionPoints);
    }
}
