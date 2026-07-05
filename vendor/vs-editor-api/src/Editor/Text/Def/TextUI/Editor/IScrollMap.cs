//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// <para>Defines the mapping between character positions and scrollmap coordinates. This is not
    /// the same as the coordinate system in which the scrollbar is rendered.</para>
    /// </summary>
    /// <remarks>
    /// <para>Valid text positions range are [0...TextView.TextSnapshot.Length].</para>
    /// <para>Corresponding scrollmap coordinates are [0.0 ... CoordinateOfBufferEnd].</para>
    /// <para>Not every buffer position will have a distinct scrollmap coordinate. For example, every character on the same line of text will,
    /// generally, have the same scrollmap coordinate.</para>
    /// <para>Different scrollmap coordinates may map to the same buffer position. For example, scrollmap coordinates in the range [0.0, 1.0) will, generally,
    /// map to the first character of the buffer.</para>
    /// </remarks>
    public interface IScrollMap : IVerticalFractionMap
    {
        /// <summary>
        /// Gets the scrollmap coordinates of a buffer position.
        /// </summary>
        /// <param name="bufferPosition">The buffer position.</param>
        /// <returns>The scrollmap coordinate, which will be between 0.0 and CoordinateOfBufferEnd inclusive.</returns>
        double GetCoordinateAtBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Does the coordinate system used by this scroll map act as if all elisions are expanded?
        /// </summary>
        bool AreElisionsExpanded { get; }

        /// <summary>
        /// Gets the buffer position that corresponds to a scrollmap coordinate.
        /// </summary>
        /// <param name="coordinate">The scrollmap coordinate.</param>
        /// <returns>The corresponding buffer position.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="coordinate"/> is NaN.</exception>
        /// <remarks>Different buffer positions can have the same scroll map coordinates. This method is guaranteed only to be consistent: it will
        /// return the same position for the same coordinate. The exact character returned will depend on the implementation of the scroll map.
        /// It will generally be the first character on the line.</remarks>
        SnapshotPoint GetBufferPositionAtCoordinate(double coordinate);

        /// <summary>
        /// The scrollmap coordinate of the start of the buffer.
        /// </summary>
        double Start { get; }

        /// <summary>
        /// The scrollmap coordinate of the end of the buffer.
        /// </summary>
        double End { get; }

        /// <summary>
        /// Gets the size of the text visible in the view (in scrollmap coordinates).
        /// </summary>
        /// <remarks>
        /// This is equivalent to the scrollbar thumb size. The total height of the scroll map, in scrollmap coordinates, 
        /// is CoordinateOfBufferEnd + ThumbSize.
        /// </remarks>
        double ThumbSize { get; }
    }
}
