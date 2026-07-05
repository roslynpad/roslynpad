//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// Used in conjunction with <see cref="IDifferenceBuffer.SnapshotDifferenceChanging"/> and
    /// <see cref="IDifferenceBuffer.SnapshotDifferenceChanged"/>.
    /// </summary>
    public class SnapshotDifferenceChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Create a change event from the given before and after <see cref="ISnapshotDifference"/>s.
        /// </summary>
        /// <param name="before">The <see cref="ISnapshotDifference"/> before the change (may be <c>null</c>).</param>
        /// <param name="after">The <see cref="ISnapshotDifference"/> after the change.</param>
        public SnapshotDifferenceChangeEventArgs(ISnapshotDifference before, ISnapshotDifference after)
        {
            Before = before; After = after;
        }

        /// <summary>
        /// The <see cref="ISnapshotDifference"/> before the change, which is <c>null</c> for the
        /// first change event.
        /// </summary>
        public ISnapshotDifference Before { get; private set; }

        /// <summary>
        /// The <see cref="ISnapshotDifference"/> after the change.
        /// If this is a <see cref="IDifferenceBuffer.SnapshotDifferenceChanging"/>
        /// event, this property will be <c>null</c>, as it hasn't been computed yet.
        /// </summary>
        public ISnapshotDifference After { get; private set; }
    }
}
