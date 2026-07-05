//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Represents a vertical scroll bar.
    /// </summary>
    public interface IVerticalScrollBar
    {
        /// <summary>
        /// Gets the mapping between the text position and the scrollbar coordinate for the scrollbar.
        /// </summary>
        IScrollMap Map { get; }

        /// <summary>
        /// Gets the y-coordinate in the scrollbar track that corresponds to a buffer position.
        /// </summary>
        /// <param name="bufferPosition">Desired position.</param>
        /// <returns>Corresponding y-coordinate.</returns>
        double GetYCoordinateOfBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets the y-coordinate in the scrollbar track that corresponds to a position in scroll map coordinates.
        /// </summary>
        /// <param name="scrollMapPosition">Desired position.</param>
        /// <returns>Corresponding y-coordinate.</returns>
        double GetYCoordinateOfScrollMapPosition(double scrollMapPosition);

        /// <summary>
        /// Gets the buffer position that corresponds to a y-coordinate in the scrollbar track.
        /// </summary>
        /// <param name="y">Desired y-coordinate.</param>
        /// <returns>A position in the buffer, even if <paramref name="y"/> is below or above the mapped range of coordinates.</returns>
        SnapshotPoint GetBufferPositionOfYCoordinate(double y);

        /// <summary>
        /// Gets the height of the scrollbar thumb in pixels.
        /// </summary>
        /// <remarks>
        /// The last buffer position maps to the bottom of the scrollbar track minus the thumb height.
        /// </remarks>
        double ThumbHeight { get; }

        /// <summary>
        /// Gets the y-coordinate of the top of the scrollbar track as it is rendered in the display (excluding the scroll buttons
        /// at the top and bottom).
        /// </summary>
        /// <remarks>
        /// <para>If mapping from scrollbar coordinates to positions in the scrollbar's track, the correct mapping is:</para>
        /// <para>pixel position = (scrollbar coordinate * TrackSpanHeight / (Map.Maximum + Map.ViewportSize)) + TrackSpanTop</para>
        /// <para>scrollbar coordinate = (pixel position - TrackSpanTop) * (Map.Maximum + Map.ViewportSize) / TrackSpanHeight</para>
        /// </remarks>
        double TrackSpanTop { get; }

        /// <summary>
        /// Gets the y-coordinate of the bottom of the scrollbar track as it is rendered in the display (excluding the scroll buttons
        /// at the top and bottom).
        /// </summary>
        double TrackSpanBottom { get; }

        /// <summary>
        /// Gets the height of the scrollbar track as it is rendered in the display (excluding the scroll buttons
        /// at the top and bottom).
        /// </summary>
        double TrackSpanHeight { get; }

        /// <summary>
        /// Occurs when the span of the scrollbar track dimensions is changed. For example, they could change as a result of resizing
        /// the view.
        /// </summary>
        event EventHandler TrackSpanChanged;
    }
}
