//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Describes a version of an <see cref="ITextBuffer"/>. Each application of an <see cref="ITextEdit"/> to a text buffer
    /// generates a new ITextVersion.
    /// </summary>
    public interface ITextVersion
    {
        /// <summary>
        /// Gets the next <see cref="ITextVersion"/>. Returns null if and only if this is the most recent version of its text buffer.
        /// </summary>
        ITextVersion Next { get; }

        /// <summary>
        /// Gets the length in characters of this <see cref="ITextVersion"/>.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the text changes that produce the next version. Returns null if and only if this is the most recent version of its text buffer.
        /// </summary>
        INormalizedTextChangeCollection Changes { get; }

        /// <summary>
        /// Creates a <see cref="ITrackingPoint"/> against this version.
        /// </summary>
        /// <param name="position">The position of the point.</param>
        /// <param name="trackingMode">The tracking mode of the point.</param>
        /// <returns>A non-null <see cref="ITrackingPoint"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than the length of this version.</exception>
        ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode);

        /// <summary>
        /// Creates a <see cref="ITrackingPoint"/> against this version.
        /// </summary>
        /// <param name="position">The position of the point.</param>
        /// <param name="trackingMode">The tracking mode of the point.</param>
        /// <param name="trackingFidelity">The tracking fidelity of the point.</param>
        /// <returns>A non-null <see cref="ITrackingPoint"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than the length of the snapshot.</exception>
        /// <remarks>This text point reprises its previous position when visiting a version that was created by undo or redo.</remarks>
        ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);

        /// <summary>
        /// Creates a <see cref="ITrackingSpan"/> against this version.
        /// </summary>
        /// <param name="span">The span of text in this snapshot that the <see cref="ITrackingSpan"/> should represent.</param>
        /// <param name="trackingMode">How the <see cref="ITrackingSpan"/> will react to changes at its boundaries.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than the length of this version, or
        /// <paramref name="trackingMode"/> is equal to <see cref="SpanTrackingMode.Custom"/>.</exception>
        ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode);

        /// <summary>
        /// Creates a <see cref="ITrackingSpan"/> against this version.
        /// </summary>
        /// <param name="span">The span of text in this snapshot that the <see cref="ITrackingSpan"/> should represent.</param>
        /// <param name="trackingMode">How the <see cref="ITrackingSpan"/> will react to changes at its boundaries.</param>
        /// <param name="trackingFidelity">The tracking fidelity of the span.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>, or
        /// <paramref name="trackingMode"/> is equal to <see cref="SpanTrackingMode.Custom"/>.</exception>
        ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);

        /// <summary>
        /// Creates a <see cref="ITrackingSpan"/> against this version.
        /// </summary>
        /// <param name="start">The starting position of the <see cref="ITrackingSpan"/> in this version.</param>
        /// <param name="length">The length of the <see cref="ITrackingSpan"/> in this version.</param>
        /// <param name="trackingMode">How the <see cref="ITrackingSpan"/> will react to changes at its boundaries.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is negative or greater than the length of this version, or
        /// <paramref name="length"/> is negative, or <paramref name="start"/> + <paramref name="length"/>
        /// is less than <paramref name="start"/>, or
        /// <paramref name="trackingMode"/> is equal to <see cref="SpanTrackingMode.Custom"/>.</exception>
        ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode);

        /// <summary>
        /// Creates a <see cref="ITrackingSpan"/> against this version.
        /// </summary>
        /// <param name="start">The starting position of the <see cref="ITrackingSpan"/> in this snapshot.</param>
        /// <param name="length">The length of the <see cref="ITrackingSpan"/> in this snapshot.</param>
        /// <param name="trackingMode">How the <see cref="ITrackingSpan"/> will react to changes at its boundaries.</param>
        /// <param name="trackingFidelity">The tracking fidelity mode.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is negative or greater than <see cref="Length"/>, or
        /// <paramref name="length"/> is negative, or <paramref name="start"/> + <paramref name="length"/>
        /// is less than <paramref name="start"/>, or
        /// <paramref name="trackingMode"/> is equal to <see cref="SpanTrackingMode.Custom"/>.</exception>
        ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);

        /// <summary>
        /// Creates a custom <see cref="ITrackingSpan"/> against this version.
        /// </summary>
        /// <param name="span">The span of text in this snapshot that the <see cref="ITrackingSpan"/> should represent.</param>
        /// <param name="trackingFidelity">The tracking fidelity of the span.</param>
        /// <param name="customState">Client-defined state associated with the span.</param>
        /// <param name="behavior">The custom tracking behavior.</param>
        /// <returns>A non-null <see cref="ITrackingSpan"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        ITrackingSpan CreateCustomTrackingSpan(Span span, TrackingFidelityMode trackingFidelity, object customState, CustomTrackToVersion behavior);

        /// <summary>
        /// The <see cref="ITextBuffer"/> to which this <see cref="ITextVersion"/> applies.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// The version number for this version. It is used for comparisons between versions of the same buffer.
        /// </summary>
        int VersionNumber { get; }

        /// <summary>
        /// Gets the oldest version number for which all text changes between that version and this version have
        /// been canceled out by corresponding undo/redo operations.
        /// </summary>
        /// <remarks>
        /// If ReiteratedVersionNumber is not equal to <see cref="VersionNumber" />, then for every 
        /// <see cref="ITextChange" /> not originated by an undo operation between ReiteratedVersionNumber and VersionNumber, there is a
        /// corresponding <see cref="ITextChange"/> originated by an undo operation that cancels it out.  So the contents of the two 
        /// versions are necessarily identical.
        ///<para>
        /// Setting this property correctly is the responsibility of the undo system; aside from this
        /// property, the text buffer and related classes are unaware of undo and redo.
        /// </para>
        /// <para>
        /// Note that the <see cref="ITextVersion"/> objects created through <see cref="ITextBuffer.ChangeContentType"/>
        /// have no text changes and will therefore keep the ReiteratedVersionNumber of the
        /// previous version.
        /// </para>
        /// </remarks>
        int ReiteratedVersionNumber { get; }
    }
}
