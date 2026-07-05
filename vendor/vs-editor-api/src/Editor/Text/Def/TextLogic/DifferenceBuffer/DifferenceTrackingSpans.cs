//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// Tracking spans for an <see cref="ISnapshotDifference"/> for the various line and word differences.
    /// </summary>
    public interface IDifferenceTrackingSpanCollection
    {
        /// <summary>
        /// Removed line spans, against the <see cref="IDifferenceBuffer.LeftBuffer"/>.
        /// </summary>
        IEnumerable<ITrackingSpan> RemovedLineSpans { get; }

        /// <summary>
        /// Removed word spans, against the <see cref="IDifferenceBuffer.LeftBuffer"/>.
        /// </summary>
        IEnumerable<ITrackingSpan> RemovedWordSpans { get; }


        /// <summary>
        /// Added line spans, against the <see cref="IDifferenceBuffer.RightBuffer"/>.
        /// </summary>
        IEnumerable<ITrackingSpan> AddedLineSpans { get; }

        /// <summary>
        /// Added line spans, against the <see cref="IDifferenceBuffer.RightBuffer"/>.
        /// </summary>
        IEnumerable<ITrackingSpan> AddedWordSpans { get; }
    }
}
