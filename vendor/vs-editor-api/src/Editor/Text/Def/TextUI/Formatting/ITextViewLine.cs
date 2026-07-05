//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using System;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Represents text that has been formatted for display in a text view.
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
    public interface ITextViewLine : IDisposable
    {
        #region Methods

        /// <summary>
        /// Gets the buffer position of the character whose character bounds contains the given x-coordinate.
        /// </summary>
        /// <param name="xCoordinate">The x-coordinate of the desired character.</param>
        /// <param name="textOnly">If true, then this method will return null if <paramref name="xCoordinate"/> is over an adornment.</param>
        /// <returns>The text buffer-based point of the character at x, or null if there is no character at that position.</returns>
        /// <remarks>
        /// <para>
        /// Please note that the rightmost edge of a character bound is considered to be contained in its following character.
        /// </para>
        /// <para>
        /// The rightmost edge of the last character's bounds don't map to any character.
        /// </para>
        /// <para>
        /// If <paramref name="textOnly"/> is true and <paramref name="xCoordinate"/> is over an adornment, then the text position assoicated with the adornment is returned.
        /// </para>
        /// </remarks>
        SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate, bool textOnly);

        /// <summary>
        /// Gets the buffer position of the character whose character bounds contains the given x-coordinate.
        /// </summary>
        /// <param name="xCoordinate">The x-coordinate of the desired character.</param>
        /// <returns>The text buffer-based point of the character at x, or null if there is no character at that position.</returns>
        /// <remarks>
        /// <para>
        /// This is equivalent to GetBufferPositionFromXCoordinate(xCoordinate, false).
        /// </para>
        /// </remarks>
        SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate);



        /// <summary>
        /// Gets the buffer position of the character whose character bounds contains the given x-coordinate.
        /// </summary>
        /// <param name="xCoordinate">The x-coordinate of the desired character.</param>
        /// <returns>The text buffer-based point of the character at x</returns>
        /// <remarks>
        /// <para>
        /// If there are no characters at the provided x-coordinate, a point in virtual space will be returned.
        /// </para>
        /// <para>
        /// If the provided x-coordinate is to the left of the start of the line, the buffer position of the line's
        /// left edge will be returned.
        /// </para>
        /// </remarks>
        VirtualSnapshotPoint GetVirtualBufferPositionFromXCoordinate(double xCoordinate);

        /// <summary>
        /// Gets the buffer position used if new data were to be inserted at the given x-coordinate.
        /// </summary>
        /// <param name="xCoordinate">The x-coordinate of the desired point.</param>
        /// <remarks>
        /// <para>
        /// If there are no characters at the provided x-coordinate, a point in virtual space will be returned.
        /// </para>
        /// <para>
        /// If the provided x-coordinate is to the left of the start of the line, the buffer position of the line's
        /// left edge will be returned.
        /// </para>
        /// </remarks>
        VirtualSnapshotPoint GetInsertionBufferPositionFromXCoordinate(double xCoordinate);

        /// <summary>
        /// Determines whether the specified buffer position lies within this text line.
        /// </summary>
        /// <param name="bufferPosition">The buffer position.</param>
        /// <returns><c>true</c> if <paramref name="bufferPosition"/> lies within this text line, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method handles the special processing required for the last line of the buffer.
        /// </remarks>
        bool ContainsBufferPosition(SnapshotPoint bufferPosition);

        /// <summary>
        /// Gets the span whose text elementindex corresponds to the given buffer position.
        /// </summary>
        /// <param name="bufferPosition">The buffer position.</param>
        /// <returns>The <see cref="SnapshotSpan"/> that corresponds to the given text element.</returns>
        SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition);

        /// <summary>
        /// Calculates the bounds of the character at the specified buffer position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer-based index of the character.
        /// </param>
        /// <returns>
        /// A <see cref="TextBounds"/> structure.
        /// </returns>
        /// <remarks>Bi-directional text will have a leading edge that lies to the right of its trailing edge.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> does not correspond to a position on this line.</exception>
        TextBounds GetCharacterBounds(SnapshotPoint bufferPosition);

        /// <summary>
        /// Calculates the bounds of the character at the specified buffer position.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer-based index of the character.
        /// </param>
        /// <returns>
        /// A <see cref="TextBounds"/> structure.
        /// </returns>
        /// <remarks>Bi-directional text will have a leading edge that lies to the right of its trailing edge.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> does not correspond to a position on this line.</exception>
        TextBounds GetCharacterBounds(VirtualSnapshotPoint bufferPosition);

        /// <summary>
        /// Calculates the bounds of the character at the specified buffer position, including any adjacent
        /// space-negotiating adornments.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer-based index of the character.
        /// </param>
        /// <returns>
        /// A <see cref="TextBounds"/> structure.
        /// </returns>
        /// <remarks>Bi-directional text will have a leading edge that lies to the right of its trailing edge.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> does not correspond to a position on this line.</exception>
        TextBounds GetExtendedCharacterBounds(SnapshotPoint bufferPosition);

        /// <summary>
        /// Calculates the bounds of the character at the specified virtual buffer position, including any adjacent
        /// space-negotiating adornments.
        /// </summary>
        /// <param name="bufferPosition">
        /// The text buffer-based index of the character.
        /// </param>
        /// <returns>
        /// A <see cref="TextBounds"/> structure.
        /// </returns>
        /// <remarks>Bi-directional text will have a leading edge that lies to the right of its trailing edge.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferPosition"/> does not correspond to a position on this line.</exception>
        TextBounds GetExtendedCharacterBounds(VirtualSnapshotPoint bufferPosition);

        /// <summary>
        /// Calculates the bounds of the specified adornment.
        /// </summary>
        /// <param name="identityTag">
        /// The <c>IAdornmentElement.IdentityTag</c> of the adornment whose bounds should be calculated.
        /// </param>
        /// <returns>
        /// A <see cref="TextBounds"/> structure if this line contains an adornment with the specified <paramref name="identityTag"/>,
        /// otherwise null.
        /// </returns>
        TextBounds? GetAdornmentBounds(object identityTag);

        /// <summary>
        /// Gets a collection of <see cref="TextBounds"/> structures for the text that corresponds to the given span.
        /// </summary>
        /// <param name="bufferSpan">
        /// The <see cref="SnapshotSpan"/> representing the text for which to compute the text bounds.
        /// </param>
        /// <returns>
        /// A collection of <see cref="TextBounds"/> structures that contain the text specified in <paramref name="bufferSpan"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the line contains bidirectional text, the <see cref="TextBounds"/> structures that are returned may be disjoint.
        /// </para>
        /// <para>
        /// The height and top of the bounds will correspond to the top and bottom of this <see cref="ITextViewLine"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSpan"/> is not a legal span in the underlying text buffer.</exception>
        Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan);

        /// <summary>
        /// Gets a tag that can be used to track the identity of an <see cref="ITextViewLine"/> across layouts in the view.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If an <see cref="ITextViewLine"/> has the same identity tag as the <see cref="ITextViewLine"/> from an earlier layout,
        /// then both text view lines correspond to the same text, even when the
        /// text has been moved without being modifed, or when the text view lines appear at different locations
        /// in the view.
        /// </para>
        /// <para>
        /// This property can be called even when the <see cref="ITextViewLine"/> is invalid.
        /// </para>
        /// </remarks>
        object IdentityTag { get; }

        /// <summary>
        /// Determines whether a <paramref name="bufferSpan"/> intersects this text line.
        /// </summary>
        /// <param name="bufferSpan">The buffer span.</param>
        /// <returns><c>true</c> if <paramref name="bufferSpan"/> intersects the text line, otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method handles the special processing required for the last line of the buffer.
        /// </remarks>
        bool IntersectsBufferSpan(SnapshotSpan bufferSpan);

        /// <summary>
        /// Gets the adornments positioned on the line.
        /// </summary>
        /// <param name="providerTag">The identity tag of the provider.
        /// This tag should match <c>SpaceNegotiatingAdornmentTag.ProviderTag</c>.</param>
        /// <returns>A sequence of adornment identity tags in order of their appearance on the line. The collection is always non-null but may be empty.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="providerTag "/> is null.</exception>
        ReadOnlyCollection<object> GetAdornmentTags(object providerTag);

        /// <summary>
        /// Sets the Change property for this text line.
        /// </summary>
        /// <param name="change">The <see cref="TextViewLineChange"/>.</param>
        void SetChange(TextViewLineChange change);

        /// <summary>
        /// Sets the position used to format the text in this formatted text line.
        /// </summary>
        /// <param name="top">The position for the top of the formatted text line.</param>
        /// <exception cref="ObjectDisposedException">This <see cref="ITextViewLine"/> has been disposed.</exception>
        void SetTop(double top);

        /// <summary>
        /// Sets the change in the position of the top of this formatted text line in the current
        /// view layout and the previous view layour.
        /// </summary>
        /// <param name="deltaY">The new deltaY value for the formatted text line.</param>
        void SetDeltaY(double deltaY);
        #endregion // Methods

        #region Properties

        /// <summary>
        /// Gets the <see cref="ITextSnapshot"/> on which this map is based.
        /// </summary>
        ITextSnapshot Snapshot { get; }

        /// <summary>
        /// Determines whether this <see cref="ITextViewLine"/> is the first line in the list of lines formatted for a particular
        /// <see cref="ITextSnapshotLine"/>.
        /// </summary>
        /// <remarks>This property will always be <c>true</c> for lines that are not word-wrapped.</remarks>
        bool IsFirstTextViewLineForSnapshotLine { get; }

        /// <summary>
        /// Determines whether this <see cref="ITextViewLine"/> is the last line in the list of lines formatted for a particular
        /// <see cref="ITextSnapshotLine"/>.
        /// </summary>
        /// <remarks>This property will always be <c>true</c> for lines that are not word-wrapped.</remarks>
        bool IsLastTextViewLineForSnapshotLine { get; }

        /// <summary>
        /// Gets the distance from the top of the text to the baseline text on the line.
        /// </summary>
        double Baseline { get; }

        /// <summary>
        /// Gets the extent of the line, excluding any line break characters.
        /// </summary>
        SnapshotSpan Extent { get; }

        /// <summary>
        /// Gets the <see cref="IMappingSpan"/> that corresponds to the <see cref="Extent"/> of the line.
        /// </summary>
        IMappingSpan ExtentAsMappingSpan { get; }

        /// <summary>
        /// Gets the extent of the line, including any line break characters.
        /// </summary>
        SnapshotSpan ExtentIncludingLineBreak { get; }

        /// <summary>
        /// Gets the <see cref="IMappingSpan"/> that corresponds to <see cref="ExtentIncludingLineBreak"/>.
        /// </summary>
        IMappingSpan ExtentIncludingLineBreakAsMappingSpan { get; }

        /// <summary>
        /// Gets the position in <see cref="Snapshot"/> of the first character in the line.
        /// </summary>
        SnapshotPoint Start { get; }

        /// <summary>
        /// Gets the length of the line, excluding any line break characters.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the length of the line, including any line break characters.
        /// </summary>
        int LengthIncludingLineBreak { get; }

        /// <summary>
        ///  Gets the position of the first character past the end of the line, excluding any
        /// line break characters. In most cases this property references a line break character, except 
        /// for the last line in the buffer, in which case it contains a
        /// position past the end of the buffer.
        /// </summary>
        SnapshotPoint End { get; }

        /// <summary>
        /// Gets the position of the first character past the end of the line, including any
        /// line break characters In most cases this property references the first character in 
        /// the following line, unless this is the last line, in which case it contains a
        /// position past the end of the buffer.
        /// </summary>
        SnapshotPoint EndIncludingLineBreak { get; }

        /// <summary>
        /// Gets the length of the line break sequence (for example, "\r\n") that appears at the end of this line.
        /// </summary>
        /// <value>A integer in the range [0..2].</value>
        /// <remarks>
        /// If this <see cref="ITextViewLine"/> corresponds to a line that was word-wrapped, then the length of its
        /// line break will be zero. The length of the line break will also be zero for the last line in the buffer.
        /// </remarks>
        int LineBreakLength
        {
            get;
        }

        /// <summary>
        /// Gets the position of the left edge of this line in the text rendering coordinate system.
        /// </summary>
        double Left
        {
            get;
        }

        /// <summary>
        /// Gets the position of the top edge of this line in the text rendering coordinate system.
        /// </summary>
        double Top
        {
            get;
        }

        /// <summary>
        /// Gets the distance between the top and bottom edge of this line.
        /// </summary>
        double Height
        {
            get;
        }

        /// <summary>
        /// Gets the y-coordinate of the top of the text in the rendered line.
        /// </summary>
        double TextTop
        {
            get;
        }

        /// <summary>
        /// Gets the y-coordinate of the bottom of the text in the rendered line.
        /// </summary>
        double TextBottom
        {
            get;
        }

        /// <summary>
        /// Gets the vertical distance between the top and bottom of the text in the rendered line.
        /// </summary>
        double TextHeight
        {
            get;
        }

        /// <summary>
        /// Gets the x-coordinate of the left edge of the text in the rendered line.
        /// </summary>
        /// <remarks>This will always be the same as <see cref="Left"/>.</remarks>
        double TextLeft
        {
            get;
        }

        /// <summary>
        /// Gets the x-coordinate of the right edge of the text in the rendered line.
        /// </summary>
        /// <remarks>This does not include the <see cref="EndOfLineWidth"/> for lines that have a line break.</remarks>
        double TextRight
        {
            get;
        }

        /// <summary>
        /// Gets the horizontal distance between <see cref="TextRight"/> and <see cref="TextLeft"/>.
        /// </summary>
        double TextWidth
        {
            get;
        }

        /// <summary>
        /// Gets the distance between the left and right edges of this line.
        /// </summary>
        double Width
        {
            get;
        }

        /// <summary>
        /// Gets the position of the bottom edge of this line in the text rendering coordinate system.
        /// </summary>
        double Bottom
        {
            get;
        }

        /// <summary>
        /// Gets the position of the right edge of this line in the text rendering coordinate system.
        /// </summary>
        double Right
        {
            get;
        }

        /// <summary>
        /// Gets the distance from the right edge of the last character in this line to
        /// the end of the space of this line. This may include padding for line break
        /// characters or for end of file characters.
        /// </summary>
        double EndOfLineWidth
        {
            get;
        }

        /// <summary>
        /// Get the width of the virtual spaces at the end of this line.
        /// </summary>
        double VirtualSpaceWidth
        {
            get;
        }

        /// <summary>
        /// Determines whether this text view line is still valid.
        /// </summary>
        bool IsValid
        {
            get; 
        }

        /// <summary>
        /// Gets the <see cref="LineTransform"/> used to render this line.
        /// </summary>
        LineTransform LineTransform { get; }

        /// <summary>
        /// Gets the default <see cref="LineTransform"/> used to render this line.
        /// </summary>
        /// <remarks>
        /// This is the line transform used if no other extension defines a <see cref="LineTransform"/> for the line.</remarks>
        LineTransform DefaultLineTransform { get; }

        /// <summary>
        /// Gets the visibility state of this rendered text line with respect to the top and bottom of the view.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This <see cref="ITextViewLine"/> has been disposed.</exception>
        VisibilityState VisibilityState { get; }

        /// <summary>
        /// Gets the change in the top of this rendered textline between between the value of <see cref="Top"/>
        /// in the current layout and the value of <see cref="Top"/> in the previous layout.
        /// </summary>
        /// <remarks>This property is 0.0 for rendered text lines that did not exist in the
        /// previous layout.</remarks>
        double DeltaY
        {
            get;
        }

        /// <summary>
        /// Gets the change to this rendered textline between the current layout and
        /// the previous layout.
        /// </summary>
        TextViewLineChange Change
        {
            get;
        }

        #endregion // Properties

        /// <summary>
        /// Sets the <see cref="ITextSnapshot"/>s upon which this formatted text line is based.
        /// </summary>
        /// <param name="visualSnapshot">the new snapshot for the line in the view model's visual buffer.</param>
        /// <param name="editSnapshot">the new snapshot for the line in the view model's edit buffer.</param>
        /// <remarks>The length of this text line is not allowed to change as a result of changing the snapshot.</remarks>
        /// <exception cref="ObjectDisposedException">This <see cref="ITextViewLine"/> has been disposed.</exception>
        void SetSnapshot(ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot);

        /// <summary>
        /// Sets the line transform used to format the text in this formatted text line.
        /// </summary>
        /// <param name="transform">The line transform for this formatted text line.</param>
        /// <exception cref="ObjectDisposedException">This <see cref="ITextViewLine"/> has been disposed.</exception>
        void SetLineTransform(LineTransform transform);
    }
}
