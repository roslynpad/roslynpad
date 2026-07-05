//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using Microsoft.VisualStudio.Text.Projection;
    using System;

    /// <summary>
    /// Creates a sequence of text and adornment elements to compose an <see cref="ITextSnapshotLine"/>.  
    /// </summary>
    public interface ITextAndAdornmentSequencer
    {
        /// <summary>
        /// Gets the <see cref="IBufferGraph"/> used by the sequencer.
        /// </summary>
        IBufferGraph BufferGraph { get; }

        /// <summary>
        /// Gets the visual <see cref="ITextBuffer"/> of the buffer graph.
        /// </summary>
        ITextBuffer TopBuffer { get; }

        /// <summary>
        /// Gets the edit buffer of the buffer graph.
        /// </summary>
        ITextBuffer SourceBuffer { get; }

        /// <summary>
        /// Creates a sequence of text and adornment elements that compose the specified <see cref="ITextSnapshotLine"/>.
        /// </summary>
        /// <param name="topLine">The <see cref="ITextSnapshotLine"/> in the <see cref="TopBuffer"/> to sequence.</param>
        /// <param name="sourceTextSnapshot">The <see cref="ITextSnapshot"/> of the <see cref="SourceBuffer"/> that corresponds to topLine.</param>
        /// <returns>A normalized collection of <see cref="ISequenceElement"/> objects that contain the text and adornment elements.</returns>
        ITextAndAdornmentCollection CreateTextAndAdornmentCollection(ITextSnapshotLine topLine, ITextSnapshot sourceTextSnapshot);

        /// <summary>
        /// Creates a sequence of text and adornment elements that compose the specified <see cref="SnapshotSpan"/>.
        /// </summary>
        /// <param name="topSpan">The <see cref="SnapshotSpan"/> in the <see cref="TopBuffer"/> to sequence.</param>
        /// <param name="sourceTextSnapshot">The <see cref="ITextSnapshot"/> of the <see cref="SourceBuffer"/> that corresponds to topSpan.</param>
        /// <returns>A normalized collection of <see cref="ISequenceElement"/> objects that contain the text and adornment elements.</returns>
        ITextAndAdornmentCollection CreateTextAndAdornmentCollection(SnapshotSpan topSpan, ITextSnapshot sourceTextSnapshot);

        /// <summary>
        /// Occurs when there has been a change in the data used by the sequencer.
        /// </summary>
        event EventHandler<TextAndAdornmentSequenceChangedEventArgs> SequenceChanged;
    }
}