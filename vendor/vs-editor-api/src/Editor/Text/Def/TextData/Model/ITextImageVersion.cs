//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Describes a version of an <see cref="ITextImage"/>.
    /// </summary>
    public interface ITextImageVersion
    {
        /// <summary>
        /// Gets the next <see cref="ITextImageVersion"/>. Returns null if and only if this is the most recent version of its <see cref="ITextImage"/>.
        /// </summary>
        ITextImageVersion Next { get; }

        /// <summary>
        /// Gets the length in characters of this <see cref="ITextImageVersion"/>.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the text changes that produce the next version. Returns null if and only if this is the most recent version of its <see cref="ITextImage"/>.
        /// </summary>
        INormalizedTextChangeCollection Changes { get; }

        /// <summary>
        /// The version number for this version.
        /// </summary>
        /// <remarks>This starts at zero and is increased as new <see cref="ITextImage"/>s that are based on this <see cref="ITextImage"/> are created.</remarks>
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
        /// Note that the <see cref="ITextImageVersion"/> created through through an operation that makes no text changes
        /// will therefore have the ReiteratedVersionNumber of the previous version.
        /// </para>
        /// </remarks>
        int ReiteratedVersionNumber { get; }

        /// <summary>
        /// A unique identifier associated with a version and all versions derived from it.
        /// </summary>
        object Identifier { get; }

        /// <summary>
        /// Translate a position in another <see cref="ITextImageVersion"/> to this <see cref="ITextImageVersion"/>.
        /// </summary>
        int TrackTo(VersionedPosition other, PointTrackingMode mode);

        /// <summary>
        /// Translate a span in another <see cref="ITextImageVersion"/> to this <see cref="ITextImageVersion"/>.
        /// </summary>
        Span TrackTo(VersionedSpan span, SpanTrackingMode mode);
    }
}
