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
    /// Provides information for an edit transaction on a <see cref="IProjectionBuffer"/> in which the set of source <see cref="ITrackingSpan"/> objects has changed.
    /// </summary>
    public class ProjectionSourceSpansChangedEventArgs : TextContentChangedEventArgs
    {
        private ReadOnlyCollection<ITrackingSpan> insertedSpans;
        private ReadOnlyCollection<ITrackingSpan> deletedSpans;
        private int spanPosition;

        /// <summary>
        /// Initializes a new instance of a <see cref="ProjectionSourceSpansChangedEventArgs"/>.
        /// </summary>
        /// <param name="beforeSnapshot">The most recent <see cref="IProjectionSnapshot"/> before the change occurred.</param>
        /// <param name="afterSnapshot">The <see cref="IProjectionSnapshot"/> immediately after the change occurred.</param>
        /// <param name="insertedSpans">Zero or more source spans that were inserted into the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="deletedSpans">Zero or more source spans that were deleted from the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="spanPosition">The position at which the span changes occurred.</param>
        /// <param name="options">The edit options that were applied to this change.</param>
        /// <param name="editTag">An arbitrary object associated with this change.</param>
        /// <exception cref="ArgumentNullException">One of the parameters: <paramref name="beforeSnapshot"/>, <paramref name="afterSnapshot"/>,
        /// <paramref name="insertedSpans"/>, or <paramref name="deletedSpans"/>is null.</exception>
        public ProjectionSourceSpansChangedEventArgs(IProjectionSnapshot beforeSnapshot,
                                                     IProjectionSnapshot afterSnapshot,
                                                     IList<ITrackingSpan> insertedSpans,
                                                     IList<ITrackingSpan> deletedSpans,
                                                     int spanPosition,
                                                     EditOptions options,
                                                     object editTag)
          : base(beforeSnapshot, afterSnapshot, options, editTag)
        {
            if (insertedSpans == null)
            {
                throw new ArgumentNullException(nameof(insertedSpans));
            }
            if (deletedSpans == null)
            {
                throw new ArgumentNullException(nameof(deletedSpans));
            }
            this.insertedSpans = new ReadOnlyCollection<ITrackingSpan>(insertedSpans);
            this.deletedSpans = new ReadOnlyCollection<ITrackingSpan>(deletedSpans);
            this.spanPosition = spanPosition;
        }

        /// <summary>
        /// The position in the list of source spans at which the change occurred.
        /// </summary>
        public int SpanPosition
        {
            get { return this.spanPosition; }
        }

        /// <summary>
        /// The set of source spans that were inserted into the <see cref="IProjectionBuffer"/> by this edit transaction.
        /// </summary>
        public ReadOnlyCollection<ITrackingSpan> InsertedSpans
        {
            get { return this.insertedSpans; }
        }

        /// <summary>
        /// The set of source spans that were deleted from the <see cref="IProjectionBuffer"/> by this edit transaction.
        /// </summary>
        public ReadOnlyCollection<ITrackingSpan> DeletedSpans
        {
            get { return this.deletedSpans; }
        }

        /// <summary>
        /// The state of the <see cref="IProjectionBuffer"/> before the change occurred.
        /// </summary>
        public new IProjectionSnapshot Before
        {
            get { return (IProjectionSnapshot)base.Before; }
        }

        /// <summary>
        /// The state of the <see cref="IProjectionBuffer"/> after the change occurred.
        /// </summary>
        public new IProjectionSnapshot After
        {
            get { return (IProjectionSnapshot)base.After; }
        }
    }
}