//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// A span of text in an <see cref="ITextBuffer"/> that grows or shrinks 
    /// with changes to the text buffer. The span may be empty.
    /// </summary>
    public interface ITrackingSpan
    {
        /// <summary>
        /// The <see cref="ITextBuffer"/> to which this tracking span refers.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// The <see cref="TrackingMode"/> of this tracking span, which determines how it behaves when insertions occur at its edges.
        /// </summary>
        SpanTrackingMode TrackingMode { get; }

        /// <summary>
        /// The <see cref="TrackingFidelityMode"/> of the tracking span, which determines how it behaves when moving to a previous version or when
        /// encountering versions that are replications of previous versions (due to undo or redo).
        /// </summary>
        TrackingFidelityMode TrackingFidelity { get; }

        /// <summary>
        /// Maps the tracking span to a particular snapshot of its text buffer.
        /// </summary>
        /// <param name="snapshot">The snapshot to which to map the tracking span.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is not a snapshot of <see cref="TextBuffer"/>.</exception>
        SnapshotSpan GetSpan(ITextSnapshot snapshot);

        /// <summary>
        /// Maps the TrackingSpan to a particular version of its text buffer.
        /// </summary>
        /// <param name="version">The version to which to map the tracking span.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="version"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="version"/> is not a version of <see cref="TextBuffer"/>.</exception>
        Span GetSpan(ITextVersion version);

        /// <summary>
        /// Maps the tracking span to a particular snapshot of its text buffer and gets the text it designates.
        /// </summary>
        /// <param name="snapshot">The snapshot to which to map the tracking span.</param>
        /// <returns>The contents of the tracking span in the specified text snapshot.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is not a snapshot of <see cref="TextBuffer"/>.</exception>
        string GetText(ITextSnapshot snapshot);

        /// <summary>
        /// Maps the start of the tracking span to a particular snapshot of its text buffer.
        /// </summary>
        /// <param name="snapshot">The snapshot to which to map the start point.</param>
        /// <returns>A <see cref="SnapshotPoint"/> of the provided snapshot.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is not a snapshot of <see cref="TextBuffer"/>.</exception>
        SnapshotPoint GetStartPoint(ITextSnapshot snapshot);

        /// <summary>
        /// Maps the end of the tracking span to a particular snapshot of its text buffer.
        /// </summary>
        /// <param name="snapshot">The snapshot to which to map the end point.</param>
        /// <returns>A <see cref="SnapshotPoint"/> of the provided snapshot.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is not a snapshot of <see cref="TextBuffer"/>.</exception>
        SnapshotPoint GetEndPoint(ITextSnapshot snapshot);
    }
}
