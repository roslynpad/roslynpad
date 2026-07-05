//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// An immutable variation on the StringBuilder class.
    /// </summary>
    internal abstract class StringRebuilder
    {
        public readonly static StringRebuilder Empty = new StringRebuilderForString();

#if DEBUG
        protected static int _totalCharactersScanned = 0;
        public static int TotalCharactersScanned { get { return _totalCharactersScanned; } }

        protected static int _totalCharactersReturned = 0;
        public static int TotalCharactersReturned { get { return _totalCharactersReturned; } }

        protected static int _totalCharactersCopied = 0;
        public static int TotalCharactersCopied { get { return _totalCharactersCopied; } }
#endif

        public static StringRebuilder Create(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
#if DEBUG
            Interlocked.Add(ref _totalCharactersScanned, text.Length);
#endif

            return (text.Length == 0)
                   ? StringRebuilder.Empty
                   : StringRebuilderForString.Create(text, text.Length, LineBreakManager.CreateLineBreaks(text));
        }

        public static StringRebuilder Create(ITextImage image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            var cti = image as CachingTextImage;
            if (cti != null)
                return cti.Builder;

            // This shouldn't happen but as a fallback, create a new string rebuilder from the text of the provided image.
            return StringRebuilder.Create(image.GetText(0, image.Length));
        }

        /// <summary>
        /// Consolidate two string rebuilders, taking advantage of the fact that they have already extracted the line breaks.
        /// </summary>
        public static StringRebuilder Consolidate(StringRebuilder left, StringRebuilder right)
        {
            Debug.Assert(left.Length > 0);
            Debug.Assert(right.Length > 0);

            int length = left.Length + right.Length;
            char[] result = new char[length];

            left.CopyTo(0, result, 0, left.Length);
            right.CopyTo(0, result, left.Length, right.Length);

            ILineBreaks lineBreaks;
            if ((left.LineBreakCount == 0) && (right.LineBreakCount == 0))
            {
                lineBreaks = LineBreakManager.Empty;
                //_lineBreakSpan defaults to 0, 0 which is what we want
            }
            else
            {
                ILineBreaksEditor breaks = LineBreakManager.CreateLineBreakEditor(length, left.LineBreakCount + right.LineBreakCount);

                int offset = 0;
                if ((result[left.Length] == '\n') && (result[left.Length - 1] == '\r'))
                {
                    //We have a \r\n spanning the seam ... add that as a special linebreak later.
                    offset = 1;
                }

                int leftLines = left.LineBreakCount - offset;
                for (int i = 0; (i < leftLines); ++i)
                {
                    Span extent;
                    int lineBreakLength;
                    left.GetLineFromLineNumber(i, out extent, out lineBreakLength);
                    breaks.Add(extent.End, lineBreakLength);
                }

                if (offset == 1)
                {
                    breaks.Add(left.Length - 1, 2);
                }

                for (int i = offset; (i < right.LineBreakCount); ++i)
                {
                    Span extent;
                    int lineBreakLength;
                    right.GetLineFromLineNumber(i, out extent, out lineBreakLength);
                    breaks.Add(extent.End + left.Length, lineBreakLength);
                }

                lineBreaks = breaks;
            }

            return StringRebuilderForChars.Create(result, length, lineBreaks);
        }

        protected StringRebuilder(int length, int lineBreakCount, char first, char last)
        {
            this.Length = length;
            this.LineBreakCount = lineBreakCount;
            this.FirstCharacter = first;
            this.LastCharacter = last;
        }

        /// <summary>
        /// Number of characters in this <see cref="StringRebuilder"/>.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Number of line breaks in this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <remarks>Line breaks consist of any of '\r', '\n', 0x85,
        /// or a "\r\n" pair (which is treated as a single line break).</remarks>
        public int LineBreakCount;

        public virtual int Depth => 0;

        /// <summary>
        /// The first character of the StringRebuilder. \0 for a zero length StringRebuilder.
        /// </summary>
        public readonly char FirstCharacter;

        /// <summary>
        /// The last character of the StringRebuilder. \0 for a zero length StringRebuilder.
        /// </summary>
        public readonly char LastCharacter;

#region Abstract methods
        /// <summary>
        /// Get the zero-based line number that contains <paramref name="position"/>.
        /// </summary>
        /// <param name="position">Position of the character for which to get the line number.</param>
        /// <returns>Number of the line that contains <paramref name="position"/>.</returns>
        /// <remarks>
        /// Lines are bounded by line breaks and the start and end of this <see cref="StringRebuilder"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        public abstract int GetLineNumberFromPosition(int position);

        /// <summary>
        /// Get the TextImageLine associated with a zero-based line number.
        /// </summary>
        /// <param name="lineNumber">Line number for which to get the TextImageLine.</param>
        /// <remarks>
        /// <para>The last "line" in the StringRebuilder has an implicit line break length of zero.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero or greater than <see cref="LineBreakCount"/>.</exception>
        public abstract void GetLineFromLineNumber(int lineNumber, out Span extent, out int lineBreakLength);

        /// <summary>
        /// Get the "leaf" node of the string rebuilder that contains position.
        /// </summary>
        /// <param name="position">position for which to get the leaf.</param>
        /// <param name="offset">number of characters to the left of the leaf.</param>
        /// <returns>leaf node from the string rebuilder.</returns>
        public abstract StringRebuilder GetLeaf(int position, out int offset);

        /// <summary>
        /// Character at the given index.
        /// </summary>
        /// <param name="index">Index to get the character for.</param>
        /// <returns>Character at position <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero or greater than or equal to <see cref="Length"/>.</exception>
        public abstract char this[int index] { get; }

        /// <summary>
        /// Copy a range of text to a destination character array.
        /// </summary>
        /// <param name="sourceIndex">
        /// The starting index to copy from.
        /// </param>
        /// <param name="destination">
        /// The destination array.
        /// </param>
        /// <param name="destinationIndex">
        /// The index in the destination of the first position to be copied to.
        /// </param>
        /// <param name="count">
        /// The number of characters to copy.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceIndex"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero or <paramref name="sourceIndex"/> + <paramref name="count"/> is greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="destinationIndex"/> is less than zero or <paramref name="destinationIndex"/> + <paramref name="count"/> is greater than the length of <paramref name="destination"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Write a substring of the contents of this <see cref="StringRebuilder"/> to a TextWriter.
        /// </summary>
        /// <param name="writer">TextWriter to use.</param>
        /// <param name="span">Span to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        public abstract void Write(TextWriter writer, Span span);

        /// <summary>
        /// Create a new StringRebuilder that corresponds to a substring of this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="span">span that defines the desired substring.</param>
        /// <returns>A new StringRebuilder containing the substring.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        public abstract StringRebuilder GetSubText(Span span);

        /// <summary>
        /// Get the string that contains all of the characters in the specified span.
        /// </summary>
        /// <param name="span">Span for which to get the text.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> can contain millions of characters. Be careful what you
        /// ask for: you might get it.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        public abstract string GetText(Span span);

        public abstract StringRebuilder Child(bool rightSide);
#endregion

        /// <summary>
        /// Convert a range of text to a character array.
        /// </summary>
        /// <param name="startIndex">
        /// The starting index of the range of text.
        /// </param>
        /// <param name="length">
        /// The length of the text.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than zero or <paramref name="startIndex"/> + <paramref name="length"/> is greater than <see cref="Length"/>.</exception>
        public char[] ToCharArray(int startIndex, int length)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            if ((length < 0) || (startIndex + length > this.Length) || (startIndex + length < 0))
                throw new ArgumentOutOfRangeException(nameof(length));

            char[] copy = new char[length];
            this.CopyTo(startIndex, copy, 0, length);

            return copy;
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to appending text into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="text">Text to append.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Append(string text)
        {
            return this.Insert(this.Length, text);
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to appending text into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="text">Text to append.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Append(StringRebuilder text)
        {
            return this.Insert(this.Length, text);
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to inserting text into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="position">Position at which to insert.</param>
        /// <param name="text">Text to insert.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Insert(int position, string text)
        {
            return this.Insert(position, StringRebuilder.Create(text));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to inserting text into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="position">Position at which to insert.</param>
        /// <param name="text">Text to insert.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Insert(int position, StringRebuilder text)
        {
            if ((position < 0) || (position > this.Length))
                throw new ArgumentOutOfRangeException(nameof(position));
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            return this.Assemble(Span.FromBounds(0, position), text, Span.FromBounds(position, this.Length));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to deleting text from this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="span">Span of text to delete.</param>
        /// <returns>A new StringRebuilder containing the deletion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        public StringRebuilder Delete(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException(nameof(span));

            return this.Assemble(Span.FromBounds(0, span.Start), Span.FromBounds(span.End, this.Length));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to replacing a contiguous span of characters
        /// with different text.
        /// </summary>
        /// <param name="span">
        /// Span of text in this <see cref="StringRebuilder"/> to replace.
        /// </param>
        /// <param name="text">
        /// The new text to replace the old.
        /// </param>
        /// <returns>
        /// A new string rebuilder containing the replacement.
        /// </returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Replace(Span span, string text)
        {
            return this.Replace(span, StringRebuilder.Create(text));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to replacing a contiguous span of characters
        /// with different text.
        /// </summary>
        /// <param name="span">
        /// Span of text in this <see cref="StringRebuilder"/> to replace.
        /// </param>
        /// <param name="text">
        /// The new text to replace the old.
        /// </param>
        /// <returns>
        /// A new string rebuilder containing the replacement.
        /// </returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Replace(Span span, StringRebuilder text)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException(nameof(span));
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            return this.Assemble(Span.FromBounds(0, span.Start), text, Span.FromBounds(span.End, this.Length));
        }

#region Private
        private StringRebuilder Assemble(Span left, Span right)
        {
            if (left.Length == 0)
                return this.GetSubText(right);
            else if (right.Length == 0)
                return this.GetSubText(left);
            else if (left.Length + right.Length == this.Length)
                return this;
            else
                return BinaryStringRebuilder.Create(this.GetSubText(left), this.GetSubText(right));
        }

        private StringRebuilder Assemble(Span left, StringRebuilder text, Span right)
        {
            if (text.Length == 0)
                return Assemble(left, right);
            else if (left.Length == 0)
                return (right.Length == 0) ? text : BinaryStringRebuilder.Create(text, this.GetSubText(right));
            else if (right.Length == 0)
                return BinaryStringRebuilder.Create(this.GetSubText(left), text);
            else if (left.Length < right.Length)
                return BinaryStringRebuilder.Create(BinaryStringRebuilder.Create(this.GetSubText(left), text),
                                                    this.GetSubText(right));
            else
                return BinaryStringRebuilder.Create(this.GetSubText(left),
                                                    BinaryStringRebuilder.Create(text, this.GetSubText(right)));
        }
#endregion
    }
}
