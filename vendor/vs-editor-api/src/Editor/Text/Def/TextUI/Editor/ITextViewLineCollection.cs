//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Formatting;
    using System.Collections.ObjectModel;

    /// <summary>
    /// <para>Represents a helper class for accessing the view's collection of <see cref="ITextViewLine"/> objects. The
    /// TextViewLines property on the <see cref="ITextView"/> is used to get an instance of this interface.</para>
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="ITextView"/> disposes its <see cref="ITextViewLineCollection"/>
    /// and all the <see cref="ITextViewLine"/> objects it contains every time it generates a new layout.</para>
    /// <para>Most properties and parameters that are doubles correspond to coordinates or distances in the text
    /// rendering coordinate system. In this coordinate system, x = 0.0 corresponds to the left edge of the drawing
    /// surface onto which text is rendered (x = view.ViewportLeft corresponds to the left edge of the viewport), and y = view.ViewportTop corresponds to the top edge of the viewport. The x-coordinate increases
    /// from left to right, and the y-coordinate increases from top to bottom. </para>
    /// <para>The horizontal and vertical axes of the view behave differently. When the text in the view is
    /// formatted, only the visible lines are formatted. As a result,
    /// a viewport cannot be scrolled horizontally and vertically in the same way.</para>
    /// <para>A viewport is scrolled horizontally by changing the left coordinate of the
    /// viewport so that it moves with respect to the drawing surface.</para>
    /// <para>A view can be scrolled vertically only by performing a new layout.</para>
    /// <para>Doing a layout in the view may cause the ViewportTop property of the view to change. For example, scrolling down one line will not translate any of the visible lines.
    /// Instead it will simply change the view's ViewportTop property (causing the lines to move on the screen even though their y-coordinates have not changed).</para>
    /// <para>Distances in the text rendering coordinate system correspond to logical pixels. If the text rendering
    /// surface is displayed without any scaling transform, then 1 unit in the text rendering coordinate system
    /// corresponds to one pixel on the display.</para>
    /// </remarks>
    public interface ITextViewLineCollection : IList<ITextViewLine>
	{
        /// <summary>
        /// Determines whether the specified buffer position is contained by any of the <see cref="ITextViewLine"/> objects in the collection.
        /// </summary>
        /// <param name="bufferPosition">The buffer position.</param>
        /// <returns><c>true</c> if <paramref name="bufferPosition"/> is contained by ones of the <see cref="ITextViewLine"/> objects, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method handles the special processing required for the last line of the buffer.
        /// </remarks>
        bool ContainsBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Detrmines whether the specified buffer span intersects any of the <see cref="ITextViewLine"/> objects in the collection.
        /// </summary>
        /// <param name="bufferSpan">The buffer span.</param>
        /// <returns><c>true</c> if <paramref name="bufferSpan"/> is contained by ones of the <see cref="ITextViewLine"/> objects, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method handles the special processing required for the last line of the buffer.
        /// </remarks>
        bool IntersectsBufferSpan(SnapshotSpan bufferSpan);

        /// <summary>
        /// Gets the <see cref="ITextViewLine"/> that contains the specified text buffer position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer position used to search for a text line.
        /// </param>
        /// <returns>
        /// An <see cref="ITextViewLine"/> that contains the position, or null if none exists.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> is not a valid buffer position.</exception>
        ITextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets the <see cref="ITextViewLine"/> that contains the specified y-coordinate.
        /// </summary>
        /// <param name="y">
        /// The y-coordinate in the text rendering coordinate.
        /// </param>
        /// <returns>
        /// A text line that contains the y-coordinate, or null if none exists.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="y"/> is NaN.</exception>
        ITextViewLine GetTextViewLineContainingYCoordinate(double y);

        /// <summary>
        /// Gets all of the <see cref="ITextViewLine"/> objects that intersect <paramref name="bufferSpan"/>.
        /// </summary>
        /// <param name="bufferSpan">The span.</param>
        /// <returns>A sorted collection of <see cref="ITextViewLine"/> objects that intersect the buffer span.</returns>
        /// <remarks>
        /// <para>This will return an empty list if there is no intersection between the
        /// <see cref="ITextViewLine"/> objects in this collection and <paramref name="bufferSpan"/>.</para>
        /// <para>This method handles the special processing required for the last line of the buffer.</para>
        /// </remarks>
        Collection<ITextViewLine> GetTextViewLinesIntersectingSpan(SnapshotSpan bufferSpan);

        /// <summary>
        /// Gets the span whose text element span contains the given buffer position.
        /// </summary>
        /// <param name="bufferPosition">The buffer position.</param>
        /// <returns>The <see cref="SnapshotSpan"/> that corresponds to the given text element index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> does not correspond to a position on this line.</exception>
        SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets the text bounds of the specified text buffer position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer-based index of the character.
        /// </param>
        /// <returns>
        /// A rectangular <see cref="TextBounds"/> structure.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> does not correspond to a position on this line.</exception>
        TextBounds GetCharacterBounds(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets a collection of <see cref="TextBounds"/> structures for the text that corresponds to the given span.
        /// </summary>
        /// <param name="bufferSpan">
        /// The buffer span representing the text for which to compute the text bounds.
        /// </param>
        /// <returns>
        /// A read-only collection of <see cref="TextBounds"/> structures that contain the text specified in <paramref name="bufferSpan"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the line contains bidirectional text, the <see cref="TextBounds"/> objects that are returned may be disjoint.
        /// </para>
        /// <para>
        /// The height and top of the bounds will be the maximum of the height and the minimum of the top of all text
        /// in the line.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSpan"/> is not a legal span in the underlying text buffer.</exception>
        Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan);

        /// <summary>
        /// Gets the index in the text lines of the given text view line.
        /// </summary>
        /// <param name="textLine">The <see cref="ITextViewLine"/> for which to find the index.</param>
        /// <returns>The index of the <see cref="ITextViewLine"/> in the view's TextLines list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="textLine"/> is null.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="textLine"/> has been disposed.</exception>
        int GetIndexOfTextLine(ITextViewLine textLine);

        /// <summary>
        /// Gets the first line that is not completely hidden.
        /// </summary>
        ITextViewLine FirstVisibleLine
        {
            get;
        }

        /// <summary>
        /// Gets the last line that is not completely hidden.
        /// </summary>
        ITextViewLine LastVisibleLine
        {
            get;
        }

        /// <summary>
        /// Gets the span of text contained in this <see cref="ITextViewLine"/> collection. 
        /// </summary>
        SnapshotSpan FormattedSpan
        {
            get;
        }

        /// <summary>
        /// Determines whether this <see cref="ITextViewLineCollection"/> object is still valid.
        /// </summary>
        /// <remarks>The <see cref="ITextView"/> will always invalidate the <see cref="ITextViewLineCollection"/>
        /// when performing a layout.</remarks>
        bool IsValid { get; }
    }
}
