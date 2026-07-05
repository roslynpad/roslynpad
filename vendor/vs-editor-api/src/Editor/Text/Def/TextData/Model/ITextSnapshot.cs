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
    /// Provides read access to an immutable snapshot of a <see cref="ITextBuffer"/> containing a sequence of Unicode characters. 
    /// The first character in the sequence has index zero.
    /// </summary>
    public interface ITextSnapshot
    {
        /// <summary>
        /// The <see cref="ITextBuffer"/> of which this is a snapshot.
        /// </summary>
        /// <remarks>
        /// This property always returns the same <see cref="ITextBuffer"/> object, but the <see cref="ITextBuffer"/> is not itself immutable.
        /// </remarks>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// The <see cref="IContentType"/> of the <see cref="TextBuffer"/> when this snapshot was current.
        /// </summary>
        IContentType ContentType { get; }

        /// <summary>
        /// The version of the <see cref="ITextBuffer"/> that this <see cref="ITextSnapshot"/> represents.
        /// </summary>
        /// <remarks>
        /// This property always returns the same <see cref="ITextVersion"/>. The <see cref="ITextVersion.Changes"/> property is
        /// initially null and becomes populated when it ceases to be the most recent version.
        /// </remarks>
        ITextVersion Version { get; }

        /// <summary>
        /// Gets the number of UTF-16 characters contained in the snapshot.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the positive number of lines in the snapshot. A snapshot whose <see cref="Length"/> is zero is considered to have one line.
        /// </summary>
        int LineCount { get; }

        /// <summary>
        /// Gets text from the snapshot starting at the beginning of the span and having length equal to the length of the span.
        /// </summary>
        /// <param name="span">The span to return.</param>
        /// <exception cref="ArgumentOutOfRangeException">The end of the span is greater than <see cref="Length"/>.</exception>
        /// <returns>A non-null string.</returns>
        string GetText(Span span);

        /// <summary>
        /// Gets text from the snapshot starting at <paramref name="startIndex"/> and having length equal to <paramref name="length"/>.
        /// </summary>
        /// <param name="startIndex">The starting index.</param>
        /// <param name="length">The length of text to get.</param>
        /// <returns>The string of length <paramref name="length"/> starting at <paramref name="startIndex"/> in the underlying <see cref="ITextBuffer"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than the length of the snapshot,
        /// or <paramref name="length"/> is less than zero, or <paramref name="startIndex"/> plus <paramref name="length"/> is greater than the length of the snapshot.</exception>
        string GetText(int startIndex, int length);

        /// <summary>
        /// Gets all the text in the snapshot.
        /// </summary>
        /// <returns>A non-null string.</returns>
        string GetText();

        /// <summary>
        /// Converts a range of text to a character array.
        /// </summary>
        /// <param name="startIndex">
        /// The starting index of the range of text.
        /// </param>
        /// <param name="length">
        /// The length of the text.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than the length of the snapshot, or
        /// <paramref name="length"/> is less than zero, or <paramref name="startIndex"/> plus <paramref name="length"/> is greater than the length of the snapshot.</exception>
        /// <returns>The array of characters starting at <paramref name="startIndex"/> in the underlying <see cref="ITextBuffer"/> and extend to its end.</returns>
        char[] ToCharArray(int startIndex, int length);

        /// <summary>
        /// Copies a range of text to a character array.
        /// </summary>
        /// <param name="sourceIndex">
        /// The starting index in the text snapshot.
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
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceIndex"/> is less than zero or greater than the length of the snapshot, or
        /// <paramref name="count"/> is less than zero, or <paramref name="sourceIndex"/> + <paramref name="count"/> is greater than the length of the snapshot, or
        /// <paramref name="destinationIndex"/> is less than zero, or <paramref name="destinationIndex"/> plus <paramref name="count"/> is greater than the length of <paramref name="destination"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Gets a single character at the specified position.
        /// </summary>
        /// <param name="position">The position of the character.</param>
        /// <returns>The character at <paramref name="position"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than or equal to the length of the snapshot.</exception>
        char this[int position] { get; }

        /// <summary>
        /// Creates a <see cref="ITrackingPoint"/> against this snapshot.
        /// </summary>
        /// <param name="position">The position of the point.</param>
        /// <param name="trackingMode">The tracking mode of the point.</param>
        /// <returns>A non-null TrackingPoint.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than the length of the snapshot.</exception>
        ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode);

        /// <summary>
        /// Creates a <see cref="ITrackingPoint"/> against this snapshot.
        /// </summary>
        /// <param name="position">The position of the point.</param>
        /// <param name="trackingMode">The tracking mode of the point.</param>
        /// <param name="trackingFidelity">The tracking fidelity of the point.</param>
        /// <returns>A non-null TrackingPoint.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than the length of the snapshot.</exception>
        /// <remarks>This text point reprises its previous position when visiting a version that was created by undo or redo.</remarks>
        ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);

        /// <summary>
        /// Creates a <see cref="ITrackingSpan"/> against this snapshot.
        /// </summary>
        /// <param name="span">The span of text in this snapshot.</param>
        /// <param name="trackingMode">How the tracking span will react to changes at its boundaries.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The end of the span is greater than the length of the text snapshot.</exception>
        ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode);

        /// <summary>
        /// Creates a <see cref="ITrackingSpan"/> against this snapshot.
        /// </summary>
        /// <param name="span">The span of text in this snapshot.</param>
        /// <param name="trackingMode">How the tracking span should react to changes at its boundaries.</param>
        /// <param name="trackingFidelity">The tracking fidelity of the span.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The end of the span is greater than the length of the text snapshot.</exception>
        ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);

        /// <summary>
        /// Creates a <see cref="ITrackingSpan"/> against this snapshot.
        /// </summary>
        /// <param name="start">The starting position of the tracking span.</param>
        /// <param name="length">The length of the tracking span.</param>
        /// <param name="trackingMode">How the tracking span should react to changes at its boundaries.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is negative or greater than <see cref="Length"/>, or
        /// <paramref name="length"/> is negative, or <paramref name="start"/> plus <paramref name="length"/>
        /// is less than <paramref name="start"/>.</exception>
        ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode);

        /// <summary>
        /// Creates a <see cref="ITrackingSpan"/> against this snapshot.
        /// </summary>
        /// <param name="start">The starting position of the tracking span.</param>
        /// <param name="length">The length of the tracking span.</param>
        /// <param name="trackingMode">How the tracking span should react to changes at its boundaries.</param>
        /// <param name="trackingFidelity">The tracking fidelilty mode.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>..</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is negative or greater than <see cref="Length"/>, or
        /// <paramref name="length"/> is negative, or <paramref name="start"/> plus <paramref name="length"/>
        /// is less than <paramref name="start"/>.</exception>
        ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);

        /// <summary>
        /// Gets an <see cref="ITextSnapshotLine"/> for the given line number.
        /// </summary>
        /// <param name="lineNumber">The line number.</param>
        /// <returns>A non-null <see cref="ITextSnapshotLine"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero or greater than or equal to <see cref="LineCount"/>.</exception>
        ITextSnapshotLine GetLineFromLineNumber(int lineNumber);

        /// <summary>
        /// Gets an <see cref="ITextSnapshotLine"/> for a line at the given position.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns>A non-null <see cref="ITextSnapshotLine"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than length of line.</exception>
        ITextSnapshotLine GetLineFromPosition(int position);

        /// <summary>
        /// Gets the number of the line that contains the character at the specified position.
        /// </summary>
        /// <returns>The line number of the line in which <paramref name="position"/> lies.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than Length/>.</exception>
        int GetLineNumberFromPosition(int position);

        /// <summary>
        /// An enumerator for the set of lines in the snapshot.
        /// </summary>
        IEnumerable<ITextSnapshotLine> Lines { get; }

        /// <summary>
        /// Writes a substring of the contents of the snapshot.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/> to use.</param>
        /// <param name="span">The span of text to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The end of the span is greater than the length of the snapshot.
        /// </exception>
        void Write(System.IO.TextWriter writer, Span span);

        /// <summary>
        /// Writes the contents of the snapshot.
        /// </summary>
        /// <param name="writer">The <see cref="System.IO.TextWriter"/>to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
        void Write(System.IO.TextWriter writer);
    }
}
