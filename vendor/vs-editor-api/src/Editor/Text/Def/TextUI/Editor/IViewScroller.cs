//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;

    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Represents a helper class for the <see cref="ITextView"/>, and provides basic functionality for scrolling. The
    /// <see cref="ITextView.ViewScroller"/> property of <see cref="ITextView"/> is used to get an instance of the this
    /// interface.
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
    public interface IViewScroller
    {
        /// <summary>
        /// Scrolls the viewport vertically by <paramref name="distanceToScroll"/>.
        /// </summary>
        /// <param name="distanceToScroll">
        /// The distance to scroll in the text rendering coordinate system. Positive values scroll the viewport
        /// up, and negative values scroll the viewport down.
        /// </param>
        /// <remarks>
        /// <para>This can be very slow for large numbers of pixels. You should avoid
        /// using this method to scroll more than the height of the viewport in either direction.</para>
        /// <para>The viewport always contains at least one visible line along its top edge, and the distance
        /// scrolled will be clipped to ensure that this always remains true.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="distanceToScroll"/> is NaN.</exception>
        void ScrollViewportVerticallyByPixels(double distanceToScroll);

        /// <summary>
        /// Scrolls the viewport vertically one line up or down.
        /// </summary>
        /// <param name="direction">
        /// The direction in which to scroll.
        /// </param>
        /// <remarks>
        /// <para>The viewport always contains at least one visible line along its top edge, and the distance
        /// scrolled is clipped to ensure that this always remains true.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="direction"/> is not a <see cref="ScrollDirection"/>.</exception>
        void ScrollViewportVerticallyByLine(ScrollDirection direction);

        /// <summary>
        /// Scrolls the viewport vertically by multiple lines up or down.
        /// </summary>
        /// <param name="direction">
        /// The direction in which to scroll.
        /// </param>
        /// <param name="count">
        /// The number of lines to scroll up or down.
        /// </param>
        /// <remarks>
        /// <para>The viewport always contains at least one visible line along its top edge, and the distance
        /// scrolled is clipped to ensure that this always remains true.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="direction"/> is not a <see cref="ScrollDirection"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        void ScrollViewportVerticallyByLines(ScrollDirection direction, int count);

        /// <summary>
        /// Scrolls the viewport vertically one page up or down.
        /// </summary>
        /// <param name="direction">
        /// The direction in which to scroll.
        /// </param>
        /// <returns><c>true</c> if the view contains one or more fully visible lines prior to scrolling, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// <para>When paging down, this method scrolls the view so that the line below the last fully-visible line
        /// is even with the top of the view. When paging up, this method scrolls the view so that the line 
        /// above the first fully visible line is even with or slightly above the bottom of the view. 
        /// It may be shifted up to prevent a partially-visible line at the top of the view.
        /// If there are no fully-visible lines in the view because the view is too short, 
        /// the view is scrolled by exactly the viewport height.</para>
        /// <para>The view cannot be scrolled so that there is a gap between the top of the view and the first line of text.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="direction"/> is not a <see cref="ScrollDirection"/>.</exception>
        bool ScrollViewportVerticallyByPage(ScrollDirection direction);

        /// <summary>
        /// Scrolls the viewport horizontally by <paramref name="distanceToScroll"/>.
        /// </summary>
        /// <param name="distanceToScroll">
        /// The distance to scroll the viewport in the text rendering coordinate system. Positive values
        /// scroll the viewport to the right, and negative values scroll the viewport to the left.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="distanceToScroll"/> is NaN.</exception>
        /// <remarks>
        /// A view cannot be scrolled horizontally if word wrap is enabled.
        /// If word wrap is disabled, the horizontal offset of the view must be between [0.0, max(0.0, formatted content width - viewport width)].
        /// </remarks>
        void ScrollViewportHorizontallyByPixels(double distanceToScroll);

        /// <summary>
        /// Ensures that all the text in <paramref name="span"/> is entirely visible in the view.
        /// </summary>
        /// <param name="span">The span to make visible.</param>
        /// <remarks>
        /// This is equivalent to scroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.None);
        /// </remarks>
        void EnsureSpanVisible(SnapshotSpan span);

        /// <summary>
        /// Ensures that all the text in <paramref name="span"/> is entirely visible in the view.
        /// </summary>
        /// <param name="span">The span to make visible.</param>
        /// <param name="options">The <see cref="EnsureSpanVisibleOptions"/>.</param>
        /// <remarks>
        /// The view will not be scrolled if the text in <paramref name="span"/> is completely visible. If the text in <paramref name="span"/> is partially visible,
        /// then the view will be scrolled as little as possible to make the text completely visible.
        /// If none of the text in <paramref name="span"/> was visible, then it will be centered in the view.
        /// </remarks>
        void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options);

        /// <summary>
        /// Ensures that all the text in <paramref name="span"/> is entirely visible in the view.
        /// </summary>
        /// <param name="span">The span to make visible.</param>
        /// <param name="options">The <see cref="EnsureSpanVisibleOptions"/>.</param>
        /// <remarks>
        /// The view will not be scrolled if the text in <paramref name="span"/> is completely visible. If the text in <paramref name="span"/> is partially visible,
        /// then the view will be scrolled as little as possible to make the text completely visible.
        /// If none of the text in <paramref name="span"/> was visible, then it will be centered in the view.
        /// </remarks>
        void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options);
    }

    /// <summary>
    /// Options to control the behavior of <see cref="IViewScroller"/> EnsureSpanVisible.
    /// </summary>
    [System.Flags]
    public enum EnsureSpanVisibleOptions
    {

        /// <summary>
        /// Ensure that the start of the span is visible if it is impossible to display the entire span.
        /// </summary>
        ShowStart = 0x01,

        /// <summary>
        /// Do the minimum amount of scrolling to display the span in the view.
        /// </summary>
        MinimumScroll = 0x02,

        /// <summary>
        /// Always center the span in the view.
        /// </summary>
        AlwaysCenter = 0x04,

        /// <summary>
        /// Ensure that the end of the span is visible if it is impossible to display the entire span. If none of the text
        /// in the span is currently visible, center the span in the view.
        /// </summary>
        None = 0x00
    };
}
