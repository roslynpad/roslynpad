//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using System;
    
#pragma warning disable CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    /// <summary>
    /// The bounds of a span of text in a given text line.
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
    public struct TextBounds
#pragma warning restore CA1066 // Type {0} should implement IEquatable<T> because it overrides Equals
    {
        #region Private Members
        private readonly double _leading;
        private readonly double _top;
        private readonly double _bidiWidth;
        private readonly double _height;
        private readonly double _textTop;
        private readonly double _textHeight;
        #endregion // Private Members

        /// <summary>
        /// Initializes a new instance of <see cref="TextBounds"/>.
        /// </summary>
        /// <param name="leading">
        /// The x-coordinate of the leading edge of the bounding rectangle.
        /// </param>
        /// <param name="top">
        /// The y-coordinate of the top edge of the bounding rectangle.
        /// </param>
        /// <param name="bidiWidth">;
        /// The distance between the leading and trailing edges of the bounding rectangle. This can be negative for right-to-left text.
        /// </param>
        /// <param name="height">
        /// The height of the rectangle. The height must be non-negative.
        /// </param>
        /// <param name="textTop">
        /// The top of the text, measured from the line that contains the text.
        /// </param>
        /// <param name="textHeight">
        /// The height of the text, measured from the line that contains the text.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="leading"/> or <paramref name="bidiWidth"/> is not a valid number, or
        /// <paramref name="height"/> is negative or not a valid number.</exception>
        public TextBounds(double leading, double top, double bidiWidth, double height, double textTop, double textHeight)
        {
            // Validate
            if (double.IsNaN(leading))
                throw new ArgumentOutOfRangeException(nameof(leading));
            if (double.IsNaN(top))
                throw new ArgumentOutOfRangeException(nameof(top));
            if (double.IsNaN(bidiWidth))
                throw new ArgumentOutOfRangeException(nameof(bidiWidth));
            if (double.IsNaN(height) || (height < 0.0))
                throw new ArgumentOutOfRangeException(nameof(height));
            if (double.IsNaN(textTop))
                throw new ArgumentOutOfRangeException(nameof(textTop));
            if (double.IsNaN(textHeight) || (textHeight < 0.0))
                throw new ArgumentOutOfRangeException(nameof(textHeight));

            _leading = leading;
            _top = top;
            _bidiWidth = bidiWidth;
            _height = height;
            _textTop = textTop;
            _textHeight = textHeight;
        }

        #region Public Properties

        /// <summary>
        /// Gets the position of the leading edge of the rectangle in the text rendering coordinate system.
        /// </summary>
        /// <remarks>
        /// In right-to-left text, the leading edge is to the right of the trailing edge.
        /// </remarks>
        public double Leading
        {
            get
            {
                return _leading;
            }
        }

        /// <summary>
        /// Gets the position of the top edge of the rectangle in the text rendering coordinate system.
        /// </summary>
        public double Top
        {
            get
            {
                return _top;
            }
        }
        
        /// <summary>
        /// Gets the top of the text on the line containing the text.
        /// </summary>
        public double TextTop
        {
            get
            {
                return _textTop;
            }
        }

        /// <summary>
        /// Gets the distance between the leading and trailing edges of the rectangle in the text rendering coordinate system.
        /// </summary>
        /// <remarks>
        /// This value will always be non-negative.
        /// </remarks>
        public double Width
        {
            get
            {
                return Math.Abs(_bidiWidth);
            }
        }

        /// <summary>
        /// Gets the distance between the top and bottom edges of the rectangle in the text rendering coordinate system.
        /// </summary>
        /// <remarks>
        /// This value will always be positive.
        /// </remarks>
        public double Height
        {
            get
            {
                return _height;
            }
        }

        /// <summary>
        /// Gets the height of the text on the line containing the characters.
        /// </summary>
        public double TextHeight
        {
            get
            {
                return _textHeight;
            }
        }

        /// <summary>
        /// Gets the position of the trailing edge of the rectangle in the text rendering coordinate system.
        /// </summary>
        /// <remarks>
        /// In right-to-left text, the trailing edge is positioned to the left of the leading edge.
        /// If the text has a non-zero width end of line glyph, this property includes the 
        /// width of that character.
        /// </remarks>
        public double Trailing
        {
            get
            {
                return _leading + _bidiWidth;
            }
        }

        /// <summary>
        /// Gets the position of the bottom edge of the rectangle in the text rendering coordinate system.
        /// </summary>
        public double Bottom
        {
            get
            {
                return _top + _height;
            }
        }

        /// <summary>
        /// Gets the bottom of the text on the line containing the characters.
        /// </summary>
        public double TextBottom
        {
            get
            {
                return _textTop + _textHeight;
            }
        }

        /// <summary>
        /// Gets the position of the left edge of the rectangle in the text rendering coordinate system.
        /// </summary>
        public double Left
        {
            get
            {
                return _bidiWidth >= 0.0 ? _leading : (_leading + _bidiWidth);
            }
        }

        /// <summary>
        /// Gets the position of the right edge of the rectangle in the text rendering coordinate system.
        /// </summary>
        public double Right
        {
            get
            {
                return _bidiWidth >= 0.0 ? (_leading + _bidiWidth) : _leading;
            }
        }

        /// <summary>
        /// Returns true if the bounds correspond to a right to left character
        /// </summary>
        public bool IsRightToLeft
        {
            get
            {
                return _bidiWidth < 0.0;
            }
        }

        #endregion // Public Properties

        #region Overrides

        /// <summary>
        /// Converts the <see cref="TextBounds"/> object to a string.
        /// </summary>
        public override string  ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0},{1},{2},{3}]", this.Leading, this.Top, this.Trailing, this.Bottom);
        }

        /// <summary>
        /// Gets the hash code of the see cref="TextBounds"/> object.
        /// </summary>
        public override int GetHashCode()
        {
            return _leading.GetHashCode() ^ _top.GetHashCode() ^ _bidiWidth.GetHashCode() ^ _height.GetHashCode() ^
                   _textTop.GetHashCode() ^ _textHeight.GetHashCode();
        }

        /// <summary>
        /// Determines whether two <see cref="TextBounds"/> objects are the same.
        /// </summary>
        public override bool Equals(object obj)
        {
            // Check for the obvious
            if (obj == null || !(obj is TextBounds))
                return false;

            TextBounds bounds = (TextBounds)obj;
            return bounds == this;
        }

        /// <summary>
        /// Determines whether two <see cref="TextBounds"/> objects are the same.
        /// </summary>
        public static bool operator ==(TextBounds bounds1, TextBounds bounds2)
        {
            return (bounds1._leading == bounds2._leading && bounds1._bidiWidth == bounds2._bidiWidth &&
                    bounds1._top == bounds2._top && bounds1._height == bounds2._height &&
                    bounds1._textTop == bounds2._textTop && bounds1._textHeight == bounds2._textHeight);
        }

        /// <summary>
        /// Determines whether two <see cref="TextBounds"/> objects are different.
        /// </summary>
        public static bool operator !=(TextBounds bounds1, TextBounds bounds2)
        {
            return !(bounds1 == bounds2);
        }

        #endregion // Overrides
    }
}
