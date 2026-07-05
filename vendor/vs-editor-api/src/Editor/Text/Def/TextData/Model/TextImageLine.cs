//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Immutable information about a line of text from an <see cref="ITextImage"/>.
    /// </summary>
    public struct TextImageLine : IEquatable<TextImageLine>
    {
        public readonly static TextImageLine Invalid = new TextImageLine();

        public TextImageLine(ITextImage image, int lineNumber, Span extent, int lineBreakLength)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if ((lineNumber < 0) || (lineNumber >= image.LineCount))
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            if (extent.End > image.Length)
                throw new ArgumentOutOfRangeException(nameof(extent));

            if ((lineBreakLength < 0) || (lineBreakLength > 2) || (extent.End + lineBreakLength > image.Length))
                throw new ArgumentOutOfRangeException(nameof(lineBreakLength));

            this.Image = image;
            this.LineNumber = lineNumber;
            this.Extent = extent;
            this.LineBreakLength = lineBreakLength;
        }

        /// <summary>
        /// The <see cref="ITextImage"/> in which the line appears.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "Type is readonly")]
#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly ITextImage Image;
#pragma warning restore CA1051 // Do not declare visible instance fields

        /// <summary>
        /// The extent of the line, excluding any line break characters.
        /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly Span Extent;
#pragma warning restore CA1051 // Do not declare visible instance fields

        /// <summary>
        /// The extent of the line, including any line break characters.
        /// </summary>
        public Span ExtentIncludingLineBreak { get { return new Span(this.Extent.Start, this.LengthIncludingLineBreak); } }

        /// <summary>
        /// The 0-origin line number of the line.
        /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly int LineNumber;
#pragma warning restore CA1051 // Do not declare visible instance fields

        /// <summary>
        /// The position of the first character in the line.
        /// </summary>
        public int Start { get { return this.Extent.Start; } }

        /// <summary>
        /// Length of the line, excluding any line break characters.
        /// </summary>
        public int Length { get { return this.Extent.Length; } }

        /// <summary>
        /// Length of the line, including any line break characters.
        /// </summary>
        public int LengthIncludingLineBreak { get { return this.Extent.Length + this.LineBreakLength; } }

        /// <summary>
        /// The position of the first character past the end of the line, excluding any
        /// line break characters (thus will address a line break character, except 
        /// for the last line in the buffer, in which case it addresses a
        /// position past the end of the buffer).
        /// </summary>
        public int End { get { return this.Extent.End; } }

        /// <summary>
        /// The position of the first character past the end of the line, including any
        /// line break characters (thus will address the first character in 
        /// the succeeding line, unless this is the last line, in which case it addresses a
        /// position past the end of the buffer).
        /// </summary>
        public int EndIncludingLineBreak { get { return this.Extent.End + this.LineBreakLength; } }

        /// <summary>
        /// Length of line break characters (always falls in the range [0..2]).
        /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
        public readonly int LineBreakLength;
#pragma warning restore CA1051 // Do not declare visible instance fields

        /// <summary>
        /// The text of the line, excluding any line break characters.
        /// </summary>
        public string GetText() { return this.Image.GetText(this.Extent); }

        /// <summary>
        /// The text of the line, including any line break characters.
        /// </summary>
        public string GetTextIncludingLineBreak() { return this.Image.GetText(this.ExtentIncludingLineBreak); }

        /// <summary>
        /// The string consisting of the line break characters (if any) at the
        /// end of the line. Has zero length for the last line in the buffer.
        /// </summary>
        public string GetLineBreakText() { return this.Image.GetText(new Span(this.Extent.End, this.LineBreakLength)); }

        public override int GetHashCode()
        {
            return (this.Image != null) ? (this.LineNumber ^ this.Image.GetHashCode()) : 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is TextImageLine)
            {
                var other = (TextImageLine)obj;
                return this.Equals(other);
            }

            return false;
        }

        public bool Equals(TextImageLine other)
        {
            return (other.Image == this.Image) && (other.LineNumber == this.LineNumber);
        }

        public static bool operator ==(TextImageLine left, TextImageLine right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextImageLine left, TextImageLine right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return (this.Image == null)
                   ? nameof(Invalid)
                   : string.Format(System.Globalization.CultureInfo.CurrentCulture, "v{0}[{1}, {2}+{3}]",
                                   this.Image.Version?.VersionNumber, this.Extent.Start, this.Extent.End, this.LineBreakLength);
        }
    }
}
