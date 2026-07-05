//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Information provided when a <see cref="ITextBuffer"/> is added or removed from a <see cref="IBufferGraph"/>.
    /// </summary>
    public class GraphBuffersChangedEventArgs : EventArgs
    {
        private ReadOnlyCollection<ITextBuffer> addedBuffers;
        private ReadOnlyCollection<ITextBuffer> removedBuffers;
        /// <summary>
        /// Initializes a new instance of <see cref="GraphBuffersChangedEventArgs"/> with the provided buffers.
        /// </summary>
        /// <param name="addedBuffers">The list of buffers that were added.</param>
        /// <param name="removedBuffers">The list of buffers that were removed.</param>
        /// <exception cref="ArgumentNullException">Either <paramref name="addedBuffers"/> or <paramref name="removedBuffers"/>
        /// is null.</exception>
        public GraphBuffersChangedEventArgs(IList<ITextBuffer> addedBuffers, IList<ITextBuffer> removedBuffers)
        {
            if (addedBuffers == null)
            {
                throw new ArgumentNullException(nameof(addedBuffers));
            }
            if (removedBuffers == null)
            {
                throw new ArgumentNullException(nameof(removedBuffers));
            }
            this.addedBuffers = new ReadOnlyCollection<ITextBuffer>(addedBuffers);
            this.removedBuffers = new ReadOnlyCollection<ITextBuffer>(removedBuffers);
        }

        /// <summary>
        /// The list of <see cref="ITextBuffer"/> objects that have been added to the <see cref="IBufferGraph"/>.
        /// </summary>
        public ReadOnlyCollection<ITextBuffer> AddedBuffers
        {
            get { return this.addedBuffers; }
        }

        /// <summary>
        /// The list of <see cref="ITextBuffer"/> objects that have been removed from the <see cref="IBufferGraph"/>.
        /// </summary>
        public ReadOnlyCollection<ITextBuffer> RemovedBuffers
        {
            get { return this.removedBuffers; }
        }
    }
}
