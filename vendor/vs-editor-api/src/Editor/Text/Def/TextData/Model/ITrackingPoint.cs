//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// A tracking position in an <see cref="ITextBuffer"/>.
    /// </summary>
    public interface ITrackingPoint
    {
        /// <summary>
        /// The <see cref="ITextBuffer"/> to which this point refers.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// Determines whether the tracking point shifts or remains stationary when insertions occur at its position.
        /// </summary>
        PointTrackingMode TrackingMode { get; }

        /// <summary>
        /// Determines how the tracking point behaves when moving to a previous version or when
        /// encountering versions that are replications of previous versions (due to undo or redo).
        /// </summary>
        TrackingFidelityMode TrackingFidelity { get; }

        /// <summary>
        /// Maps the tracking point to a particular snapshot of its <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="snapshot">The snapshot to which to map the tracking point.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is not a snapshot of <see cref="ITextBuffer"/>.</exception>
        SnapshotPoint GetPoint(ITextSnapshot snapshot);

        /// <summary>
        /// The position of the tracking point in the specified <see cref="ITextSnapshot"/>.
        /// </summary>
        /// <param name="snapshot">The snapshot to which to map the position.</param>
        /// <returns>An integer position in the given snapshot.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="snapshot"/> is not a snapshot of <see cref="TextBuffer"/>.</exception>
        int GetPosition(ITextSnapshot snapshot);

        /// <summary>
        /// The position of the tracking point in the specified <see cref="ITextVersion"/>.
        /// </summary>
        /// <param name="version">The version to which to map the position.</param>
        /// <returns>An integer position in the given version.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="version"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="version"/> is not a version of <see cref="TextBuffer"/>.</exception>
        int GetPosition(ITextVersion version);

        /// <summary>
        /// Maps this tracking point to the specified snapshot and gets the character at that position.
        /// </summary>
        /// <param name="snapshot">The snapshot to which to map the position.</param>
        /// <returns>The character at the specified position.</returns>
        /// <exception cref="ArgumentOutOfRangeException">This ITrackingPoint denotes the end position of the snapshot.</exception>
        char GetCharacter(ITextSnapshot snapshot);
    }

}
