//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// <para>Represents the caret associated with an <see cref="ITextView"/>.</para>
    /// </summary>
    /// <remarks>
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
    public interface ITextCaret
    {
        /// <summary>
        /// Makes the caret visible by scrolling the view up or down and left or right until the caret is visible.
        /// </summary>
        void EnsureVisible();

        /// <summary>
        /// Moves the caret to the best <see cref="CaretPosition"/> for the specified x-coordinate and text line.
        /// </summary>
        /// <param name="textLine">
        /// The text line that will contain the caret.
        /// </param>
        /// <param name="xCoordinate">
        /// The x-coordinate of the caret in the text rendering coordinate system.
        /// </param>
        /// <returns>
        /// A <see cref="CaretPosition"/> that contains the valid values of the caret after the move has occurred.
        /// </returns>
        /// <remarks>This is equivalent to calling MoveTo(textLine, xCoordinate, true).</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="xCoordinate"/> is NaN.</exception>
        CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate);

        /// <summary>
        /// Moves the caret to the best <see cref="CaretPosition"/>  for the given x-coordinate and text line.
        /// </summary>
        /// <param name="textLine">
        /// The text line that will contain the caret.
        /// </param>
        /// <param name="xCoordinate">
        /// The x-coordinate of the caret in the text rendering coordinate system.
        /// </param>
        /// <param name="captureHorizontalPosition"><c>true</c> if the caret should capture its horizontal position for subsequent moves up or down.
        /// <c>false</c> if the caret should retain its previously-captured horizontal position.</param>
        /// <returns>
        /// A <see cref="CaretPosition"/> that contains the valid values of the caret after the move has occurred.
        /// </returns>
        /// <remarks>This method takes care of UTF-16 surrogate pairs and combining character sequences.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="xCoordinate"/> is NaN.</exception>
        CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition);

        /// <summary>
        /// Moves the caret to the specified <paramref name="textLine"/> while preserving its current x-coordinate.
        /// </summary>
        /// <param name="textLine">The text line that will contain the caret.</param>
        /// <returns>
        /// A <see cref="CaretPosition"/> that contains the valid values of the caret after the move has occurred.
        /// </returns>
        CaretPosition MoveTo(ITextViewLine textLine);

        /// <summary>
        /// Moves the caret to the given index in the underlying <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="bufferPosition">The <see cref="SnapshotPoint"/> in the underlying text buffer to which
        /// to move the caret.</param>
        /// <returns>A <see cref="CaretPosition"/> that contains the valid values of the caret after the move has occurred.</returns>
        /// <remarks>This is equivalent to calling MoveTo(bufferPosition, PositionAffinity.Successor, true).</remarks>
        CaretPosition MoveTo(SnapshotPoint bufferPosition);

        /// <summary>
        /// Moves the caret to the given index in the underlying <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="bufferPosition">The <see cref="SnapshotPoint"/> in the underlying text buffer to which
        /// to move the caret.</param>
        /// <param name="caretAffinity">The affinity of the caret. This will be ignored unless 
        /// <paramref name="bufferPosition"/> specifies a location that is at the seam between two word-wrapped lines.</param>
        /// <returns>A <see cref="CaretPosition"/> that contains the valid values of the caret position after the move has occurred.</returns>
        /// <remarks>This is equivalent to calling MoveTo(bufferPosition, caretAffinity, true).</remarks>
        CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity);

        /// <summary>
        /// Moves the caret to the given index in the underlying <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="bufferPosition">The <see cref="SnapshotPoint"/> in the underlying text buffer to which
        /// to move the caret.</param>
        /// <param name="caretAffinity">The affinity of the caret. This will be ignored unless 
        /// <paramref name="bufferPosition"/> specifies a location that is at the seam between two word-wrapped lines.</param>
        /// <param name="captureHorizontalPosition"><c>true</c> if the caret should capture its horizontal position for subsequent moves up or down,
        /// <c>false</c> if the caret should retain its previously-captured horizontal position.</param>
        /// <returns>A <see cref="CaretPosition"/> that contains the valid values of the caret position after the move has occurred.</returns>
        /// <remarks>This method handles UTF-16 surrogate pairs and combining character sequences.  
        /// For example, if the text buffer consists of a high surrogate character at index 0 and a low surrogate character at index 1, 
        /// and <paramref name="bufferPosition"/> is 1 and 
        /// <paramref name="caretAffinity"/> is <see cref="PositionAffinity.Successor"/>, 
        /// the actual valid caret index is 0 (since the high surrogate and low surrogate characters form one text element). 
        /// If <paramref name="caretAffinity"/> is<see cref="PositionAffinity.Predecessor"/>, the actual valid caret index is 2.</remarks>
        CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition);

        /// <summary>
        /// Moves the caret to the specified <paramref name="bufferPosition"/>.
        /// </summary>
        /// <param name="bufferPosition">The <see cref="VirtualSnapshotPoint"/> in the underlying text buffer to which
        /// to move the caret.</param>
        /// <returns>A <see cref="CaretPosition"/> that contains the valid values of the caret position after the move has occurred.</returns>
        /// <remarks>This is equivalent to calling MoveTo(bufferPosition, PositionAffinity.Successor, true).</remarks>
        CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition);

        /// <summary>
        /// Moves the caret to the specified <paramref name="bufferPosition"/>.
        /// </summary>
        /// <param name="bufferPosition">The <see cref="VirtualSnapshotPoint"/> in the underlying text buffer to which
        /// to move the caret.</param>
        /// <param name="caretAffinity">The affinity of the caret. This will be ignored unless <paramref name="bufferPosition"/> 
        /// specifies a location that is at the seam between two word-wrapped lines.</param>
        /// <returns>A <see cref="CaretPosition"/> that contains the valid values of the caret position after the move has occurred.</returns>
        /// <remarks>This is equivalent to calling MoveTo(bufferPosition, caretAffinity, true).</remarks>
        CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity);


        /// <summary>
        /// Moves the caret to the specified <paramref name="bufferPosition"/>.
        /// </summary>
        /// <param name="bufferPosition">The <see cref="VirtualSnapshotPoint"/> in the underlying text buffer to which
        /// to move the caret.</param>
        /// <param name="caretAffinity">The affinity of the caret. This will be ignored unless <paramref name="bufferPosition"/> 
        /// specifies a location that is at the seam between two word-wrapped lines.</param>
        /// <param name="captureHorizontalPosition">If <c>true</c>, the caret will capture its horizontal position for subsequent moves up or down.
        /// If <c>false</c>, the caret retains its previously-captured horizontal position.</param>
        /// <returns>A <see cref="CaretPosition"/> that contains the valid values of the caret position after the move has occurred.</returns>
        /// <remarks>This method handles UTF-16 surrogate pairs and combining character sequences.  
        /// For example, if the text buffer consists of a high surrogate character at index 0 and a low surrogate character at index 1, 
        /// and <paramref name="bufferPosition"/> is 1 and <paramref name="caretAffinity"/> is <see cref="PositionAffinity.Successor"/>, 
        /// the actual valid caret index is 0 (since the high surrogate and low surrogate characters form one text element). 
        /// If <paramref name="caretAffinity"/> is <see cref="PositionAffinity.Predecessor"/>, the actual valid caret index is 2.</remarks>
        CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition);

        /// <summary>
        /// Moves the caret to the preferred x and y-coordinates.
        /// </summary>
        /// <returns>A <see cref="CaretPosition"/> that contains the valid values of the caret position after the move has occurred.</returns>
        /// <remarks>You cannot change the preferred coordinates by calling this method.</remarks>
        CaretPosition MoveToPreferredCoordinates();

        /// <summary>
        /// Moves the caret to the next valid <see cref="CaretPosition"/>.
        /// </summary>
        /// <returns>A <see cref="CaretPosition"/> containing the valid values of the caret after the move has occurred.</returns>
        /// <remarks>This method handles UTF-16 surrogate pairs and combining character sequences.</remarks>
        CaretPosition MoveToNextCaretPosition();

        /// <summary>
        /// Moves the caret to the previous valid <see cref="CaretPosition"/>.
        /// </summary>
        /// <returns>A <see cref="CaretPosition"/> containing the valid values of the caret after the move has occurred.</returns>
        /// <remarks>This method handles UTF-16 surrogate pairs and combining character sequences.</remarks>
        CaretPosition MoveToPreviousCaretPosition();

        /// <summary>
        /// Gets the <see cref="ITextViewLine"/> that contains the caret, provided that that text line is visible
        /// in the view.
        /// </summary>
        ITextViewLine ContainingTextViewLine { get; }

        /// <summary>
        /// Gets the position of the left edge of the caret in the text rendering coordinate system.
        /// </summary>
        double Left
        {
            get;
        }

        /// <summary>
        /// Gets the width of the caret in the text rendering coordinate system.
        /// </summary>
        double Width
        {
            get;
        }

        /// <summary>
        /// Gets the position of the right edge of the caret in the text rendering coordinate system.
        /// </summary>
        double Right
        {
            get;
        }

        /// <summary>
        /// Gets the position of the top edge of the caret in the text rendering coordinate system.
        /// </summary>
        /// <exception cref="InvalidOperationException">The caret does not lie in the text formatted by the view.</exception>
        double Top
        {
            get;
        }

        /// <summary>
        /// Gets the height of the caret in the text rendering coordinate system.
        /// </summary>
        /// <exception cref="InvalidOperationException">The caret does not lie in the text formatted by the view.</exception>
        double Height
        {
            get;
        }

        /// <summary>
        /// Gets the position of the bottom edge of the caret in the text rendering coordinate system.
        /// </summary>
        /// <exception cref="InvalidOperationException">The caret does not lie in the text formatted by the view.</exception>
        double Bottom
        {
            get;
        }

        /// <summary>
        /// Gets the current position of the caret.
        /// </summary>
        CaretPosition Position
        {
            get;
        }

        /// <summary>
        /// Determines whether the caret is in overwrite mode.
        /// </summary>
        /// <remarks>
        /// <para>When the caret is in overwrite mode, typed characters replace the character under the caret, 
        /// and a block is drawn instead of a vertical line.</para>
        /// <para>This is distinct from the IEditorOptions overwrite mode, 
        /// since the caret can switch modes based on its position in the view. 
        /// The caret is not in OverwriteMode when it is positioned at the end of the line in a view, or when there is a
        /// non-empty selection, even if IEditorOptions.OverwriteMode is true.</para>
        /// </remarks>
        bool OverwriteMode
        {
            get;
        }

        /// <summary>
        /// Determines whether the caret lies in virtual space. A virtual space is one that is after the physical end of a line.
        /// </summary>
        /// <remarks>
        /// <para>This is distinct from the <see cref="IEditorOptions"/> UseVirtualSpace, 
        /// since virtual space can be enabled even if the caret does not lie in virtual space.</para>
        /// </remarks>
        bool InVirtualSpace
        {
            get;
        }

        /// <summary>
        /// Gets or sets the visibility of the caret.
        /// </summary>
        bool IsHidden 
        { 
            get; set; 
        }

        /// <summary>
        /// Occurs when the position of the caret has been explicitly changed.
        /// </summary>
        /// <remarks>
        /// The event is not raised if the caret position was changed as a consequence of tracking normal text edits.
        /// The normal behavior of the caret is to move after the typed character.
        /// </remarks>
        event EventHandler<CaretPositionChangedEventArgs> PositionChanged;
    }
}
