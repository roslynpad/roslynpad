//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;

    /// <summary>
    /// Provides data about an edit transaction on a <see cref="IElisionBuffer"/> in which the set of hidden source spans has changed.
    /// </summary>
    public class ElisionSourceSpansChangedEventArgs : TextContentChangedEventArgs
    {
        private NormalizedSpanCollection elidedSpans;
        private NormalizedSpanCollection expandedSpans;

        /// <summary>
        /// Initialize a new instance of an <see cref="ElisionSourceSpansChangedEventArgs"/> object.
        /// </summary>
        /// <param name="beforeSnapshot">The most recent <see cref="IProjectionSnapshot"/> before the change occurred.</param>
        /// <param name="afterSnapshot">The <see cref="IProjectionSnapshot"/> immediately after the change occurred.</param>
        /// <param name="elidedSpans">Zero or more source spans that were hidden.</param>
        /// <param name="expandedSpans">Zero or more source spans that were expanded.</param>
        /// <param name="sourceToken">An arbitrary object associated with this change.</param>
        /// <exception cref="ArgumentNullException">One of <paramref name="beforeSnapshot"/>,  <paramref name="afterSnapshot"/>,
        /// <paramref name="elidedSpans"/>, or <paramref name="expandedSpans"/> is null.</exception>
        public ElisionSourceSpansChangedEventArgs(IProjectionSnapshot beforeSnapshot,
                                                  IProjectionSnapshot afterSnapshot,
                                                  NormalizedSpanCollection elidedSpans,
                                                  NormalizedSpanCollection expandedSpans,
                                                  object sourceToken)
            : base(beforeSnapshot, afterSnapshot, EditOptions.None, sourceToken)
        {
            if (elidedSpans == null)
            {
                throw new ArgumentNullException(nameof(elidedSpans));
            }
            if (expandedSpans == null)
            {
                throw new ArgumentNullException(nameof(expandedSpans));
            }
            this.elidedSpans = elidedSpans;
            this.expandedSpans = expandedSpans;
        }

        /// <summary>
        /// The set of source spans that were inserted into the <see cref="IProjectionBuffer"/> by this edit transaction.
        /// </summary>
        public NormalizedSpanCollection ElidedSpans
        {
            get { return this.elidedSpans; }
        }

        /// <summary>
        /// The set of source spans that were deleted from the <see cref="IProjectionBuffer"/> by this edit transaction.
        /// </summary>
        public NormalizedSpanCollection ExpandedSpans
        {
            get { return this.expandedSpans; }
        }

        /// <summary>
        /// The state of the <see cref="IProjectionBuffer"/> before the change occurred.
        /// </summary>
        public new IProjectionSnapshot Before
        {
            get { return (IProjectionSnapshot)base.Before; }
        }

        /// <summary>
        /// The state of the <see cref="IProjectionBuffer"/> after the change.
        /// </summary>
        public new IProjectionSnapshot After
        {
            get { return (IProjectionSnapshot)base.After; }
        }
    }
}