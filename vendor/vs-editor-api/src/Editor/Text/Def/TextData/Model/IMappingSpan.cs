//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using Microsoft.VisualStudio.Text.Projection;

    /// <summary>
    /// A span in a <see cref="ITextBuffer"/> that can be mapped within a <see cref="Morgania.Text.Projection.IBufferGraph"/>.
    /// </summary>
    public interface IMappingSpan
    {
        /// <summary>
        /// Maps the span to a particular <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="targetBuffer">The <see cref="ITextBuffer"/> to which to map the span.</param>
        /// <returns>The possibly empty collection of spans in the <paramref name="targetBuffer"/> to which the span maps.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="targetBuffer"/> is null.</exception>
        NormalizedSnapshotSpanCollection GetSpans(ITextBuffer targetBuffer);

        /// <summary>
        /// Maps the span to a particular <see cref="ITextSnapshot"/>.
        /// </summary>
        /// <param name="targetSnapshot">The <see cref="ITextSnapshot"/> to which to map the span.</param>
        /// <returns>The possibly empty collection of spans in the <paramref name="targetSnapshot"/> to which the span maps.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="targetSnapshot"/> is null.</exception>
        NormalizedSnapshotSpanCollection GetSpans(ITextSnapshot targetSnapshot);

        /// <summary>
        /// Maps the span to a matching <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="match">The predicate used to identify the <see cref="ITextBuffer"/>.</param>
        /// <returns>A possibly empty collection of spans in the matching buffer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
        /// <remarks><paramref name="match"/> is called on each text buffer in the buffer graph until it
        /// returns <c>true</c>. The predicate will not be called again.</remarks>
        NormalizedSnapshotSpanCollection GetSpans(Predicate<ITextBuffer> match);

        /// <summary>
        /// Gets the <see cref="IMappingPoint"/> for the start of this span.
        /// </summary>
        IMappingPoint Start { get; }

        /// <summary>
        /// Gets the <see cref="IMappingPoint"/> for the end of this span.
        /// </summary>
        IMappingPoint End { get; }

        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> from which this span was created.
        /// </summary>
        ITextBuffer AnchorBuffer { get; }

        /// <summary>
        /// Gets the <see cref="IBufferGraph"/> that this span uses to perform mapping.
        /// </summary>
        IBufferGraph BufferGraph { get; }

    }
}