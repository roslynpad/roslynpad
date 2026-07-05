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
    /// Provides information for an edit transaction on a <see cref="IProjectionBuffer"/> in which the set of source <see cref="ITextBuffer"/> objects has changed.
    /// </summary>
    public class ProjectionSourceBuffersChangedEventArgs : ProjectionSourceSpansChangedEventArgs
    {
        private IList<ITextBuffer> addedBuffers;
        private IList<ITextBuffer> removedBuffers;

        /// <summary>
        /// Initializes a new instance of a <see cref="ProjectionSourceBuffersChangedEventArgs"/> object.
        /// </summary>
        /// <param name="beforeSnapshot">The most recent <see cref="IProjectionSnapshot"/> before the change occurred.</param>
        /// <param name="afterSnapshot">The <see cref="IProjectionSnapshot"/> immediately after the change occurred.</param>
        /// <param name="insertedSpans">Zero or more source spans that were inserted into the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="deletedSpans">Zero or more source spans that were deleted from the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="spanPosition">The position in the list of source spans at which the buffer changes occurred.</param>
        /// <param name="addedBuffers">The list of added source <see cref="ITextBuffer"/> objects.</param>
        /// <param name="removedBuffers">The list of removed source <see cref="ITextBuffer"/> objects.</param>
        /// <param name="options">The edit options that were applied to this change.</param>
        /// <param name="editTag">An arbitrary object associated with this change.</param>
        /// <exception cref="ArgumentNullException"><paramref name="insertedSpans"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="deletedSpans"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="addedBuffers"/> or <paramref name="removedBuffers"/> is null.</exception>
        public ProjectionSourceBuffersChangedEventArgs(IProjectionSnapshot beforeSnapshot,
                                                       IProjectionSnapshot afterSnapshot,
                                                       IList<ITrackingSpan> insertedSpans,
                                                       IList<ITrackingSpan> deletedSpans,
                                                       int spanPosition,
                                                       IList<ITextBuffer> addedBuffers, 
                                                       IList<ITextBuffer> removedBuffers,
                                                       EditOptions options,
                                                       object editTag)
          : base(beforeSnapshot, afterSnapshot, insertedSpans, deletedSpans, spanPosition, options, editTag)
        {
            if (addedBuffers == null)
            {
                throw new ArgumentNullException(nameof(addedBuffers));
            }
            if (removedBuffers == null)
            {
                throw new ArgumentNullException(nameof(removedBuffers));
            }
            this.addedBuffers = addedBuffers;
            this.removedBuffers = removedBuffers;
        }

        /// <summary>
        /// The source buffers that were added to the projection buffer.
        /// </summary>
        public ReadOnlyCollection<ITextBuffer> AddedBuffers
        {
            get { return new ReadOnlyCollection<ITextBuffer>(this.addedBuffers); }
        }

        /// <summary>
        /// The source buffers that were removed and no longer contribute spans to the projection buffer.
        /// </summary>
        public ReadOnlyCollection<ITextBuffer> RemovedBuffers
        {
            get { return new ReadOnlyCollection<ITextBuffer>(this.removedBuffers); }
        }
    }
}
