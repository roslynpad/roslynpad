//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;

    /// <summary>
    /// A restricted projection buffer that has exactly one source buffer. Spans from the source buffer
    /// must appear in the same order in the projection buffer as in the source buffer.
    /// </summary>
    /// <remarks>
    /// The source spans of an elision buffer are all <see cref="SpanTrackingMode.EdgeInclusive"/>. Consequently,
    /// if all of the contents of a source span are deleted, and later an insertion is made at the location of that span
    /// in the source buffer, the insertion will appear in the elision buffer.
    /// </remarks>
    public interface IElisionBuffer : IProjectionBufferBase
    {
        /// <summary>
        /// Gets the source buffer of this elision buffer.
        /// </summary>
        ITextBuffer SourceBuffer { get; }

        /// <summary>
        /// Gets the current snapshot of this elision buffer.
        /// </summary>
        new IElisionSnapshot CurrentSnapshot { get; }

        /// <summary>
        /// Hides the text designated by <paramref name="spansToElide"/>. 
        /// </summary>
        /// <param name="spansToElide">The spans of text to hide with respect to the current snapshot of the source buffer. 
        /// It is not an error if some of the designated text is already hidden. These spans are converted to EdgeExclusive
        /// tracking spans.</param>
        /// <returns>A newly generated snapshot.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="spansToElide"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The end of the final span in <paramref name="spansToElide"/> is greater 
        /// than <see cref="SourceBuffer"/>.CurrentSnapshot.Length.</exception>
        IProjectionSnapshot ElideSpans(NormalizedSpanCollection spansToElide);

        /// <summary>
        /// Expands the text specified by <paramref name="spansToExpand"/>.
        /// </summary>
        /// <param name="spansToExpand">The spans of text to expand, with respect to the current snapshot of the source buffer.
        /// It is not an error if some of the designated text is already expanded.</param>
        /// <exception cref="ArgumentNullException"><paramref name="spansToExpand"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The end of the final spans in <paramref name="spansToExpand"/> is greater 
        /// than <see cref="SourceBuffer"/>.CurrentSnapshot.Length.</exception>
        IProjectionSnapshot ExpandSpans(NormalizedSpanCollection spansToExpand);

        /// <summary>
        /// Modifies the exposed text by hiding <paramref name="spansToElide"/> and then expanding <paramref name="spansToExpand"/>
        /// in a single transaction.
        /// </summary>
        /// <param name="spansToElide">The spans of text to hide with respect to the current snapshot of the source buffer. 
        /// It is not an error if some of the designated text is already hidden. These spans are converted to EdgeExclusive
        /// tracking spans. This parameter may be null.</param>
        /// <param name="spansToExpand">The spans of text to expand, with respect to the current snapshot of the source buffer.
        /// It is not an error if some of the designated text is already expanded. This parameter may be null.</param>
        /// <exception cref="ArgumentOutOfRangeException">The end of the final spans in <paramref name="spansToElide"/> or 
        /// <paramref name="spansToExpand"/> is greater than <see cref="SourceBuffer"/>.CurrentSnapshot.Length.</exception>
        IProjectionSnapshot ModifySpans(NormalizedSpanCollection spansToElide, NormalizedSpanCollection spansToExpand);

        /// <summary>
        /// Gets the <see cref="ElisionBufferOptions"/> in effect for this <see cref="IElisionBuffer"/>.
        /// </summary>
        ElisionBufferOptions Options { get; }

        /// <summary>
        /// Occurs when the set of hidden spans changes.
        /// </summary>
        event EventHandler<ElisionSourceSpansChangedEventArgs> SourceSpansChanged;
    }
}
