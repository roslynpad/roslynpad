//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;

    /// <summary>
    /// A snapshot from an <see cref="IElisionBuffer"/> object.
    /// </summary>
    public interface IElisionSnapshot : IProjectionSnapshot
    {
        /// <summary>
        /// Gets the <see cref="IElisionBuffer"/> of which this is a snapshot.
        /// </summary>
        /// <remarks>
        /// This property always returns the same elision buffer, but that elision buffer is not itself immutable.
        /// </remarks>
        new IElisionBuffer TextBuffer { get; }

        /// <summary>
        /// Gets the text snapshot on which this elision snapshot is based.
        /// </summary>
        ITextSnapshot SourceSnapshot { get; }

        /// <summary>
        /// Maps from a snapshot point in the source buffer to the corresponding point in the elision snapshot.
        /// If the source buffer position is not exposed in the elision snapshot, returns the nearest point that is
        /// exposed. If nothing is exposed, returns position zero.
        /// </summary>
        /// <param name="point">The snapshot point in a source buffer to map.</param>
        /// <returns>A position in the elision snapshot.</returns>
        /// <exception cref="ArgumentException"><paramref name="point"/> does not belong to the source snapshot of this elision snapshot.</exception>
        SnapshotPoint MapFromSourceSnapshotToNearest(SnapshotPoint point);
    }
}
