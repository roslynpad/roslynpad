//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using System;

#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// Represents the transform from a formatted text line to a rendered text line.
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
    public struct LineTransform
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        private readonly double _topSpace;
        private readonly double _bottomSpace;
        private readonly double _verticalScale;
        private readonly double _right;

        /// <summary>
        /// Initializes a new instance of a <see cref="LineTransform"/>. 
        /// </summary>
        /// <param name="verticalScale">The vertical scale factor to be applied to the text of the line, but not the space above and below the line.</param>
        /// <remarks>
        /// <para>All <see cref="LineTransform"/> objects on a formatted line of text are combined using the <see cref="Combine"/> operator below. 
        /// The resulting <see cref="LineTransform"/> determines the placement and scaling of the rendered line of text.</para>
        /// </remarks>
        public LineTransform(double verticalScale)
            : this(0.0, 0.0, verticalScale, 0.0)
        {
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="LineTransform"/>.  
        /// </summary>
        /// <param name="topSpace">The amount of space required above the text of the line before applying <paramref name="verticalScale"/>.</param>
        /// <param name="bottomSpace">The amount of space required below the text of the line before applying <paramref name="verticalScale"/>.</param>
        /// <param name="verticalScale">The vertical scale factor to be applied to the text of the line, but not the space above and below the line.</param>
        /// <remarks>
        /// <para>All the <see cref="LineTransform"/> objects on a formatted line of text are combined
        /// using the <see cref="Combine"/> operator, and the combined <see cref="LineTransform"/> determines 
        /// the placement and scaling of the rendered line of text.</para>
        /// <para>Negative <paramref name="topSpace"/> and <paramref name="bottomSpace"/> values will be ignored,
        /// since they will always be combined with
        /// at least one <see cref="LineTransform"/> with non-negative space requests.</para>
        /// <para>The rendered height of a line will be 
        /// ((line text height) + <paramref name="topSpace"/> + <paramref name="bottomSpace"/>) * <paramref name="verticalScale"/>.</para>
        /// </remarks>
        public LineTransform(double topSpace, double bottomSpace, double verticalScale)
            : this(topSpace, bottomSpace, verticalScale, 0.0)
        {
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="LineTransform"/>.  
        /// </summary>
        /// <param name="topSpace">The amount of space required above the text of the line before applying <paramref name="verticalScale"/>.</param>
        /// <param name="bottomSpace">The amount of space required below the text of the line before applying <paramref name="verticalScale"/>.</param>
        /// <param name="verticalScale">The vertical scale factor to be applied to the text of the line and the space above and below the line.</param>
        /// <param name="right">The x-coordinate of the right edge the line (typically the right edge of any adornment on the line that extends to the right of the line's text).</param>
        /// <remarks>
        /// <para>All the <see cref="LineTransform"/> objects on a formatted line of text are combined
        /// using the <see cref="Combine"/> operator, and the combined <see cref="LineTransform"/> determines 
        /// the placement and scaling of the rendered line of text.</para>
        /// <para>Negative <paramref name="topSpace"/> and <paramref name="bottomSpace"/> values will be ignored,
        /// since they will always be combined with
        /// at least one <see cref="LineTransform"/> with non-negative space requests.</para>
        /// <para>The rendered height of a line will be 
        /// ((line text height) + <paramref name="topSpace"/> + <paramref name="bottomSpace"/>) * <paramref name="verticalScale"/>.</para>
        /// </remarks>
        public LineTransform(double topSpace, double bottomSpace, double verticalScale, double right)
        {
            if (double.IsNaN(topSpace))
                throw new ArgumentOutOfRangeException(nameof(topSpace));

            if (double.IsNaN(bottomSpace))
                throw new ArgumentOutOfRangeException(nameof(bottomSpace));

            if ((verticalScale <= 0.0) || double.IsNaN(verticalScale))
                throw new ArgumentOutOfRangeException(nameof(verticalScale));

            if ((right < 0.0) || double.IsNaN(right))
                throw new ArgumentOutOfRangeException(nameof(right));

            _topSpace = topSpace;
            _bottomSpace = bottomSpace;
            _verticalScale = verticalScale;
            _right = right;
        }

        /// <summary>
        /// Gets the amount of space required above the text of the line before applying the <see cref="VerticalScale"/> factor.
        /// </summary>
        public double TopSpace { get { return _topSpace; } }

        /// <summary>
        /// Gets the amount of space required below the text of the line before applying the <see cref="VerticalScale"/> factor.
        /// </summary>
        public double BottomSpace { get { return _bottomSpace; } }

        /// <summary>
        /// Gets the vertical scale factor to be applied to the text of the line. The scale factor does not affect
        /// and the space above and below the line.
        /// </summary>
        public double VerticalScale { get { return _verticalScale; } }
        
        /// <summary>
        /// Gets the x-coordinate of the effective right edge of the line.
        /// </summary>
        public double Right { get { return _right; } }

        /// <summary>
        /// Combines two <see cref="LineTransform"/> objects.
        /// </summary>
        /// <param name="transform1">The first <see cref="LineTransform"/> to combine.</param>
        /// <param name="transform2">The second <see cref="LineTransform"/> to combine.</param>
        /// <returns>The combined <see cref="LineTransform"/>.</returns>
        public static LineTransform Combine(LineTransform transform1, LineTransform transform2)
        {
            return new LineTransform(Math.Max(transform1.TopSpace, transform2.TopSpace),
                                     Math.Max(transform1.BottomSpace, transform2.BottomSpace),
                                     transform1.VerticalScale * transform2.VerticalScale,
                                     Math.Max(transform1.Right, transform2.Right));
        }

        #region Overridden methods and operators

        /// <summary>
        /// Gets the hash code for this object.
        /// </summary>
        public override int GetHashCode()
        {
            return (_topSpace.GetHashCode() ^ _bottomSpace.GetHashCode() ^ _verticalScale.GetHashCode() ^ _right.GetHashCode());
        }

        /// <summary>
        /// Determines whether two <see cref="LineTransform"/> objects are the same.
        /// </summary>
        /// <param name="obj">The object to compare for equality.</param>
        public override bool Equals(object obj)
        {
            if (obj is LineTransform)
            {
                LineTransform other = (LineTransform)obj;
                return this == other;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="LineTransform"/> objects are the same.
        /// </summary>
        public static bool operator ==(LineTransform transform1, LineTransform transform2)
        {
            return (transform1._topSpace == transform2._topSpace) &&
                   (transform1._bottomSpace == transform2._bottomSpace) &&
                   (transform1._verticalScale == transform2._verticalScale) &&
                   (transform1._right == transform2._right);
        }

        /// <summary>
        /// Determines whether two <see cref="LineTransform"/> objects are different.
        /// </summary>
        public static bool operator !=(LineTransform transform1, LineTransform transform2)
        {
            return !(transform1 == transform2);
        }

        #endregion // Overridden methods and operators
    }
}