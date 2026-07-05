//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Provides read access to an immutable sequence of Unicode characters. 
    /// The first character in the sequence has index zero.
    /// </summary>
    public interface ITextImage
    {
        /// <summary>
        /// The <see cref="ITextImageVersion"/> associated with this <see cref="ITextImage"/>.
        /// </summary>
        /// <remarks>
        /// This will be null unless this <see cref="ITextImage"/> was created by a component that
        /// manages the version information.</remarks>
        ITextImageVersion Version { get; }

        /// <summary>
        /// Create a new <see cref="ITextImage"/> that is a clone of a subspan of this <see cref="ITextImage"/>.
        /// </summary>
        /// <remarks>
        /// <para>The new <see cref="ITextImage"/> will not inherit the version or version history of this <see cref="ITextImage"/>.</para>
        /// <para>The <see cref="ITextImage.Version"/> of the returned image will be null even the contents are identical to this.</para>/// </remarks>
        ITextImage GetSubText(Span span);

        /// <summary>
        /// Gets the number of UTF-16 characters contained in the image.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the positive number of lines in the image. An image whose <see cref="Length"/> is zero is considered to have one line.
        /// </summary>
        int LineCount { get; }

        /// <summary>
        /// Gets text from the image starting at the beginning of the span and having length equal to the length of the span.
        /// </summary>
        /// <param name="span">The span to return.</param>
        /// <exception cref="ArgumentOutOfRangeException">The end of the span is greater than <see cref="Length"/>.</exception>
        /// <returns>A non-null string.</returns>
        string GetText(Span span);

        /// <summary>
        /// Converts a range of text to a character array.
        /// </summary>
        /// <param name="startIndex">
        /// The starting index of the range of text.
        /// </param>
        /// <param name="length">
        /// The length of the text.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than the length of the image, or
        /// <paramref name="length"/> is less than zero, or <paramref name="startIndex"/> plus <paramref name="length"/> is greater than the length of the image.</exception>
        /// <returns>The array of characters starting at <paramref name="startIndex"/> in the underlying <see cref="ITextBuffer"/> and extend to its end.</returns>
        char[] ToCharArray(int startIndex, int length);

        /// <summary>
        /// Copies a range of text to a character array.
        /// </summary>
        /// <param name="sourceIndex">
        /// The starting index in the text image.
        /// </param>
        /// <param name="destination">
        /// The destination array.
        /// </param>
        /// <param name="destinationIndex">
        /// The index in the destination array at which to start copying the text.
        /// </param>
        /// <param name="count">
        /// The number of characters to copy.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceIndex"/> is less than zero or greater than the length of the image, or
        /// <paramref name="count"/> is less than zero, or <paramref name="sourceIndex"/> + <paramref name="count"/> is greater than the length of the image, or
        /// <paramref name="destinationIndex"/> is less than zero, or <paramref name="destinationIndex"/> plus <paramref name="count"/> is greater than the length of <paramref name="destination"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Gets a single character at the specified position.
        /// </summary>
        /// <param name="position">The position of the character.</param>
        /// <returns>The character at <paramref name="position"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than or equal to the length of the image.</exception>
        char this[int position] { get; }

        /// <summary>
        /// Gets an <see cref="TextImageLine"/> for the given line number.
        /// </summary>
        /// <param name="lineNumber">The line number.</param>
        /// <returns>A non-null <see cref="TextImageLine"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero or greater than or equal to <see cref="LineCount"/>.</exception>
        TextImageLine GetLineFromLineNumber(int lineNumber);

        /// <summary>
        /// Gets an <see cref="TextImageLine"/> for a line at the given position.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>A non-null <see cref="TextImageLine"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than length of line.</exception>
        TextImageLine GetLineFromPosition(int position);

        /// <summary>
        /// Gets the number of the line that contains the character at the specified position.
        /// </summary>
        /// <returns>The line number of the line in which <paramref name="position"/> lies.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than Length/>.</exception>
        int GetLineNumberFromPosition(int position);

        /// <summary>
        /// Writes a substring of the contents of the image.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/> to use.</param>
        /// <param name="span">The span of text to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The end of the span is greater than the length of the image.
        /// </exception>
        void Write(System.IO.TextWriter writer, Span span);
    }

    public static class TextImageExtensions
    {
        /// <summary>
        /// Gets text from the image starting at <paramref name="startIndex"/> and having length equal to <paramref name="length"/>.
        /// </summary>
        /// <param name="startIndex">The starting index.</param>
        /// <param name="length">The length of text to get.</param>
        /// <returns>The string of length <paramref name="length"/> starting at <paramref name="startIndex"/> in the underlying <see cref="ITextBuffer"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than the length of the image,
        /// or <paramref name="length"/> is less than zero, or <paramref name="startIndex"/> plus <paramref name="length"/> is greater than the length of the image.</exception>
        public static string GetText(this ITextImage image, int startIndex, int length)
        {
            return image.GetText(new Span(startIndex, length));
        }

        /// <summary>
        /// Gets all the text in the image.
        /// </summary>
        /// <returns>A non-null string.</returns>
        /// <remarks>Caveat emptor. Calling GetText() on a 100MB <see cref="ITextImage"/> will give you exactly what you asked for, which
        /// probably isn't what you wanted.</remarks>
        public static string GetText(this ITextImage image)
        {
            return image.GetText(new Span(0, image.Length));
        }

        /// <summary>
        /// Create a new <see cref="ITextImage"/> that is a clone of a subspan of this <see cref="ITextImage"/>.
        /// </summary>
        /// <remarks>
        /// The new <see cref="ITextImage"/> will not inherit the version or version history of this <see cref="ITextImage"/>.</remarks>
        public static ITextImage GetSubText(this ITextImage image, int startIndex, int length)
        {
            return image.GetSubText(new Span(startIndex, length));
        }

        /// <summary>
        /// Writes the contents of the image.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/>to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
        public static void Write(this ITextImage image, System.IO.TextWriter writer)
        {
            image.Write(writer, new Span(0, image.Length));
        }
    }
}
