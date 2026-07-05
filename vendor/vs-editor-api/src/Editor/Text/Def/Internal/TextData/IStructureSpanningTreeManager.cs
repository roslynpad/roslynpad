//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Structure
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.UI.Adornments;

    /// <summary>
    /// Defines the interface for the <see cref="IStructureSpanningTreeManager"/> which
    /// provides information about the structural hierarchy of code in an <see cref="ITextView"/>.
    /// </summary>
    /// <remarks>
    /// You can obtain an instance of this class via the <see cref="IStructureSpanningTreeService"/>.
    /// </remarks>
    public interface IStructureSpanningTreeManager
    {
        /// <summary>
        /// Event that indicates that <see cref="SpanningTreeSnapshot"/> has been updated,
        /// and that any method calls on this service will now return more up to date results.
        /// </summary>
        event EventHandler SpanningTreeChanged;

        /// <summary>
        /// Gets an immutable instance of the most up to date current code structure.
        /// </summary>
        IStructureElement SpanningTreeSnapshot { get; }

        /// <summary>
        /// Gets an enumerable of <see cref="IStructureElement"/>s that encapsulate the given <see cref="SnapshotPoint"/>.
        /// </summary>
        /// <remarks>
        /// This method is intended as a projection-aware means to obtain language-service provided
        /// structural context for a location such as the caret position, or a structure guide line.
        /// </remarks>
        /// <param name="point">A <see cref="SnapshotPoint"/> indicating the position of interest.</param>
        /// <returns>
        /// The elements within which <paramref name="point"/> is nested, in order, from outermost to innermost.
        /// </returns>
        IEnumerable<IStructureElement> GetElementsEncapsulatingPoint(SnapshotPoint point);

        /// <summary>
        /// Gets an enumerable of <see cref="IStructureElement"/>s that intersect with the given
        /// <see cref="SnapshotSpan"/>.
        /// </summary>
        /// <remarks>
        /// This method is intended as a projection-aware means to obtain language-service provided
        /// structural context for a span.
        /// </remarks>
        /// <param name="spans">The spans to collect elements from.</param>
        /// <returns>The elements that intersect with the given span.</returns>
        IEnumerable<IStructureElement> GetElementsIntersectingSpans(NormalizedSnapshotSpanCollection spans);
    }
}
