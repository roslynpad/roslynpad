//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Provides a list of all mapping spans where tags have changed since the last BatchedTagsChanged event. 
    /// The BatchedTagsChanged event is raised on the same thread as the thread that created the tag aggregator.
    /// </summary>
    public class BatchedTagsChangedEventArgs : EventArgs
    {
        ReadOnlyCollection<IMappingSpan> _spans;

        /// <summary>
        /// Initializes a new instance of <see cref="BatchedTagsChangedEventArgs"/> with the specified list of <see cref="IMappingSpan" />s.
        /// </summary>
        /// <param name="spans">The list of <see cref="IMappingSpan" />s where the tags have changed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="spans"/> is null.</exception>
        public BatchedTagsChangedEventArgs(IList<IMappingSpan> spans)
        {
            if (spans == null)
                throw new ArgumentNullException(nameof(spans));

            //Make a copy of spans so we don't need to worry about it changing.
            _spans = new ReadOnlyCollection<IMappingSpan>(new List<IMappingSpan>(spans));
        }

        /// <summary>
        /// The list of <see cref="IMappingSpan" />s where the tags have changed.
        /// </summary>
        public ReadOnlyCollection<IMappingSpan> Spans { get { return _spans; } }
    }
}
