//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Represents a view of text in an <see cref="ITextBuffer"/>. It is the base class for a platform-specific interface
    /// that has methods to allow the formatted text to be rendered.
    /// </summary>
    /// <remarks>
    /// <para>A text view is a platform-independent representation of a contiguous block of formatted and adorned text,
    /// accessible through the <see cref="TextViewLines"/> property.
    /// It also instantiates an instance of an IEditorOperations component part so that
    /// it can execute various commands.</para>
    /// <para>The text is formatted based on the classifiers attached to the underlying <see cref="ITextBuffer"/>.</para>
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
    public interface ITextView : IPropertyOwner
    {
        #region Methods
        /// <summary>
        /// Formats and displays the contents of the text buffer so that the <see cref="ITextViewLine"/> containing <paramref name="bufferPosition"/> 
        /// is displayed at the desired position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The position of the character that is to be contained in the <see cref="ITextViewLine"/> displayed at the specified vertical position.
        /// </param>
        /// <param name="verticalDistance">
        /// The distance (in pixels) between the <see cref="ITextViewLine"/> and the edge of the view. If <paramref name="relativeTo"/> is equal to
        /// <c>ViewRelativePosition.Top</c>, then the distance is from the top of the view to the top of the <see cref="ITextViewLine"/>. Otherwise,
        /// it is the distance from the bottom of the <see cref="ITextViewLine"/> to the bottom on the view.
        /// Negative values are allowed, which might cause the line to be displayed outside the viewport.
        /// This method can become quite expensive if <paramref name="verticalDistance"/> is large. You 
        /// should avoid making <paramref name="verticalDistance"/> greater than the height of the view.
        /// </param>
        /// <param name="relativeTo">
        /// Specifies whether the line offset is relative to the top or bottom of the view.
        /// </param>
        /// <returns>
        /// The vertical distance (from the top or bottom of the view) 
        /// at which the <see cref="ITextViewLine"/> containing the specified position is to be displayed.
        /// </returns>
        /// <remarks>
        /// <para>If word wrap is disabled in the view, then the <see cref="ITextViewLine"/> 
        /// corresponds to the entire <see cref="ITextSnapshotLine"/> that contains <paramref name="bufferPosition"/>.
        /// If word wrap is enabled in the view, then the <see cref="ITextViewLine"/> 
        /// corresponds to the portion of the <see cref="ITextSnapshotLine"/> that both
        /// contains <paramref name="bufferPosition"/> and fits into the view. <paramref name="bufferPosition"/> may not be the first
        /// character in the <see cref="ITextViewLine"/>.</para>
        /// <para>The returned value will generally be equal to <paramref name="verticalDistance"/>, except in cases where the view
        /// was repositioned to prevent a gap from appearing at the top or bottom of the view.</para>
        /// <para>Calling this method will cause the view to dispose of its current <see cref="TextViewLines"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="bufferPosition"/> is from the wrong
        /// <see cref="ITextSnapshot"/> or <see cref="ITextBuffer"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="relativeTo"/> is not a valid <see cref="ViewRelativePosition"/>.</exception>
        void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo);

        /// <summary>
        /// Formats and displays the contents of the text buffer so that the <see cref="ITextViewLine"/> containing <paramref name="bufferPosition"/> 
        /// is displayed at the desired position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The position of the character that is to be contained in the <see cref="ITextViewLine"/> displayed at the specified vertical position.
        /// </param>
        /// <param name="verticalDistance">
        /// The distance (in pixels) between the <see cref="ITextViewLine"/> and the edge of the view. If <paramref name="relativeTo"/> is equal to
        /// <c>ViewRelativePosition.Top</c>, then the distance is from the top of the view to the top of the <see cref="ITextViewLine"/>. Otherwise,
        /// it is the distance from the bottom of the <see cref="ITextViewLine"/> to the bottom on the view.
        /// Negative values are allowed, which might cause the line to be displayed outside the viewport.
        /// This method can become quite expensive if <paramref name="verticalDistance"/> is large. You 
        /// should avoid making <paramref name="verticalDistance"/> greater than the height of the view.
        /// </param>
        /// <param name="relativeTo">
        /// Specifies whether the line offset is relative to the top or bottom of the view.
        /// </param>
        /// <param name="viewportWidthOverride">
        /// If specified, the text is formatted as if the viewport had the specified width.
        /// </param>
        /// <param name="viewportHeightOverride">
        /// If specified, the text is formatted as if the viewport had the specified height.
        /// </param>
        /// <returns>
        /// The vertical distance (from the top or bottom of the view) 
        /// at which the <see cref="ITextViewLine"/> containing the specified position is to be displayed.
        /// </returns>
        /// <remarks>
        /// <para>If word wrap is disabled in the view, then the <see cref="ITextViewLine"/> 
        /// corresponds to the entire <see cref="ITextSnapshotLine"/> that contains <paramref name="bufferPosition"/>.
        /// If word wrap is enabled in the view, then the <see cref="ITextViewLine"/> 
        /// corresponds to the portion of the <see cref="ITextSnapshotLine"/> that both
        /// contains <paramref name="bufferPosition"/> and fits into the view. <paramref name="bufferPosition"/> may not be the first
        /// character in the <see cref="ITextViewLine"/>.</para>
        /// <para>The returned value will generally be equal to <paramref name="verticalDistance"/>, except in cases where the view
        /// was repositioned to prevent a gap from appearing at the top or bottom of the view.</para>
        /// <para>Calling this method will cause the view to dispose of its current <see cref="TextViewLines"/>.</para>
        /// <para>The viewport width override will have no effect unless word wrap is enabled in the view.</para>
        /// <para>The viewport height and width overrides only change how text is formatted for this call. Subsequent calls will use the
        /// width and height of the viewport (unless explicitly overriden a second time).</para>
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="bufferPosition"/> is from the wrong
        /// <see cref="ITextSnapshot"/> or <see cref="ITextBuffer"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="relativeTo"/> is not a valid <see cref="ViewRelativePosition"/>.</exception>
        void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo,
                                                     double? viewportWidthOverride, double? viewportHeightOverride);

        /// <summary>
        /// Gets the <see cref="SnapshotSpan"/> of text that constitutes a text element (a single visual representation)
        /// at the given <see cref="SnapshotPoint"/>.
        /// </summary>
        /// <param name="point">The <see cref="SnapshotPoint"/> in the text snapshot at which to get the text element.</param>
        /// <returns>A <see cref="SnapshotSpan"/> containing the bounds of the text element.</returns>
        /// <exception cref="ArgumentException"><paramref name="point"/> is from the wrong
        /// <see cref="ITextBuffer"/>.</exception>
        /// <remarks>A text element may be a UTF-16 surrogate pair, consisting of a high
        /// surrogate character and a low surrogate character. If a point in the text buffer
        /// lies between a high surrogate character and a low surrogate character, the text element span will 
        /// start at the high surrogate character and end at the low surrogate character.</remarks>
        SnapshotSpan GetTextElementSpan(SnapshotPoint point);

        /// <summary>
        /// Closes the text view and its view.
        /// </summary>
        /// <exception cref="InvalidOperationException">The text view host is already closed.</exception>
        void Close();

        /// <summary>
        /// Requests a refresh of the space reservation stack.
        /// </summary>
        /// <remarks>
        /// Refreshing the space reservation stack involves asking each of the space reservation managers/agents to reposition
        /// themselves.  This method will be called mostly by space reservation agents that wish to reposition their content.  The
        /// space reservation stack is refreshed asynchronously.  Calling QueueSpaceReservationStackRefresh will perform a refresh
        /// of the space reservation stack, but the effects will not be visible immediately on return of the call.
        /// </remarks>
        void QueueSpaceReservationStackRefresh();

        #endregion // Methods

        #region Properties

        /// <summary>
        /// Determines whether the view is in the process of being laid out.
        /// </summary>
        /// <remarks>Attempting to get the text view lines of the view while it is being laid out will throw an exception.</remarks>
        bool InLayout
        {
            get;
        }

        /// <summary>
        /// Gets a helper that provides various methods to scroll or manipulate the view.
        /// </summary>
        IViewScroller ViewScroller
        {
            get;
        }

        /// <summary>
        /// Gets a read-only list of the <see cref="ITextViewLine"/> objects rendered in this view.
        /// </summary>
        /// <remarks>
        /// This list will be dense. That is, all characters between the first character of the first <see cref="ITextViewLine"/> through
        /// the last character of the last <see cref="ITextViewLine"/> will be represented in one of the <see cref="ITextViewLine"/> objects,
        /// except when the layout of the <see cref="ITextViewLine"/> objects is in progress.
        /// <para>
        /// <see cref="ITextViewLine"/> objects are disjoint. That is, a given character is part of only one <see cref="ITextViewLine"/>.
        /// </para>
        /// <para>
        /// The <see cref="ITextViewLine"/> objects are sorted by the index of their first character.
        /// </para>
        /// <para>Some of the <see cref="ITextViewLine"/> objects may not be visible, 
        /// and all <see cref="ITextViewLine"/> objects will be disposed of when the view
        /// recomputes its layout.</para>
        /// <para>This property will be null during the view's initialization.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">View is in the process of being laid out.</exception>
        ITextViewLineCollection TextViewLines
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="ITextViewLine"/> that contains the specified text buffer position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer position used to search for a text line.
        /// </param>
        /// <returns>
        /// The <see cref="ITextViewLine"/> that contains the specified buffer position.
        /// </returns>
        /// <remarks>
        /// <para>This method returns an <see cref="ITextViewLine"/> if it exists in the view.</para>
        /// <para>If the line does not exist in the cache of formatted lines, it will be formatted and added to the cache.</para>
        /// <para>The returned <see cref="ITextViewLine"/> could be invalidated by either a layout by the view or by subsequent calls to this method.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> is not a valid buffer position.</exception>
        /// <exception cref="InvalidOperationException"> if the view has not completed initialization.</exception>
        ITextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets the caret element.
        /// </summary>
        ITextCaret Caret
        {
            get;
        }

        /// <summary>
        /// Gets the selection element.
        /// </summary>
        ITextSelection Selection
        {
            get;
        }

        /// <summary>
        /// Gets the span of text covered by the provisional text highlight.
        /// </summary>
        /// <remarks>
        /// If there is no provisional text, this method returns null.
        /// </remarks>
        ITrackingSpan ProvisionalTextHighlight
        {
            get;
            set;
        }

        /// <summary>
        /// The roles which this view plays. Roles partially determine the extensions that are instantiated for the view.
        /// </summary>
        ITextViewRoleSet Roles
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> whose text is rendered in this view.
        /// </summary>
        ITextBuffer TextBuffer
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="IBufferGraph"/> that contains the set of source buffers that contribute to this view.
        /// </summary>
        IBufferGraph BufferGraph
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="ITextSnapshot"/> of the text that is currently rendered in the view.
        /// </summary>
        /// <remarks>
        /// This snapshot will be identical to the CurrentSnapshot of <see cref="TextBuffer"/>, except when handling a
        /// Changed event on that buffer.
        /// </remarks>
        ITextSnapshot TextSnapshot
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="ITextSnapshot"/> of the visual buffer that is being rendered.
        /// </summary>
        /// <remarks>
        /// This snapshot should not be used in any method that requires a position in the text buffer, since
        /// those positions refer to <see cref="TextSnapshot"/>.
        /// </remarks>
        ITextSnapshot VisualSnapshot
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="ITextViewModel"/> of this text view.
        /// </summary>
        ITextViewModel TextViewModel
        {
            get;
        }

        /// <summary>
        /// Gets the <see cref="ITextDataModel"/> of this text view.
        /// </summary>
        ITextDataModel TextDataModel
        {
            get;
        }

        /// <summary>
        /// Gets the right coordinate of the longest line, whether or not that line is currently visible, in logical pixels.
        /// </summary>
        /// <remarks>This value is cached and may not represent the width of the widest line
        /// in the underlying buffer. For example, if the widest line has never been formatted,
        /// then it is not in the cache.</remarks>
        double MaxTextRightCoordinate
        {
            get;
        }

        /// <summary>
        /// Gets or sets the position of the left edge of the viewport in the text rendering coordinate system.
        /// </summary>
        /// <remarks>
        /// When set, the horizontal offset is clipped to [0.0, Max(0.0, formatted text width - viewport width)] on non word-wrapped views,
        /// and [0,0] for views in which word-wrap is enabled.
        /// </remarks>
        double ViewportLeft
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the position of the top edge of the viewport in the text rendering coordinate system.
        /// </summary>
        /// <remarks>
        /// Scrolling the text is done by changing the set of formatted lines and/or the vertical offset of those lines.
        /// </remarks>
        double ViewportTop
        {
            get;
        }

        /// <summary>
        /// Gets the position of the right edge of the viewport in the text rendering coordinate system.
        /// </summary>
        double ViewportRight
        {
            get;
        }

        /// <summary>
        /// Gets the position of the bottom edge of the viewport in the text rendering coordinate system.
        /// </summary>
        double ViewportBottom
        {
            get;
        }

        /// <summary>
        /// Gets the width of the visible content window in logical pixels.
        /// </summary>
        double ViewportWidth
        {
            get;
        }

        /// <summary>
        /// Gets the height of the visible content window in logical pixels.
        /// </summary>
        double ViewportHeight
        {
            get;
        }

        /// <summary>
        /// Gets the nominal height of a line of text in the view.
        /// </summary>
        double LineHeight
        {
            get;
        }

        /// <summary>
        /// Determines whether this text view has been closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Gets the options for this text view.
        /// </summary>
        IEditorOptions Options { get; }

        /// <summary>
        /// Determines whether the mouse is over the view or any of its adornments.
        /// </summary>
        bool IsMouseOverViewOrAdornments { get; }

        /// <summary>
        /// Determines whether the view or any of its adornments has focus.
        /// </summary>
        bool HasAggregateFocus { get; }

        #endregion // Properties

        #region Events

        /// <summary>
        /// Occurs whenever the text displayed in the view changes.
        /// </summary>
        /// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
        /// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
        /// <para>It ia also raised whenever the view scrolls horizontally or when its size changes.</para></remarks>
        event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;

        /// <summary>
        /// Occurs when the position of the viewport's left edge is changed. (e.g. when the view is horizontally scrolled)
        /// </summary>
        /// <remarks>Deprecated. Use LayoutChanged instead.</remarks>
        event EventHandler ViewportLeftChanged;

        /// <summary>
        /// Occurs when the viewport's height is changed.
        /// </summary>
        /// <remarks>Deprecated. Use LayoutChanged instead.</remarks>
        event EventHandler ViewportHeightChanged;

        /// <summary>
        /// Occurs when the viewport's width is changed.
        /// </summary>
        /// <remarks>Deprecated. Use LayoutChanged instead.</remarks>
        event EventHandler ViewportWidthChanged;

        /// <summary>
        /// Occurs when the mouse has hovered over the same character.
        /// </summary>
        /// <remarks>
        /// This event is raised only once, unless either the mouse moves or the text in the view changes.
        /// <para>The delay between the time when the mouse stops moving and the time when the event is raised 
        /// can be changed by adding a <see cref="MouseHoverAttribute"/> to the event handler.
        /// If no <see cref="MouseHoverAttribute"/> is specified on the event handler, the delay will be 150ms.</para>
        /// </remarks>
        event EventHandler<MouseHoverEventArgs> MouseHover;

        /// <summary>
        /// Occurs immediately after the text view is closed.
        /// </summary>
        event EventHandler Closed;

        /// <summary>
        /// Occurs when the keyboard focus switches away from the view and any of its adornments.
        /// </summary>
        /// <remarks>This event will not be raised when keyboard focus transitions from the view to one of its popups.</remarks>
        event EventHandler LostAggregateFocus;

        /// <summary>Occurs when the keyboard focus switches to the view or one of its adornments.
        /// </summary>
        event EventHandler GotAggregateFocus;

        #endregion // Events

        /// <summary>
        /// Gets a named <see cref="ISpaceReservationManager"/>.
        /// </summary>
        /// <param name="name">The name of the manager.</param>
        /// <returns>An instance of the manager in this view. Not null.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="name"/> is not registered via an <see cref="SpaceReservationManagerDefinition"/>.</exception>
        /// <remarks>
        /// <para>Managers must be exported using <see cref="SpaceReservationManagerDefinition"/> component parts.</para>
        /// </remarks>
        ISpaceReservationManager GetSpaceReservationManager(string name);
    }
}
