//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;

    /// <summary>
    /// Provides information about the <see cref="ITagAggregator&lt;T&gt;" />.TagsChanged event.
    /// </summary>
    public class TagsChangedEventArgs : EventArgs
    {   
        /// <summary>
        /// Gets the span over which tags have changed.
        /// </summary>
        public IMappingSpan Span { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="TagsChangedEventArgs"/> with the specified <see cref="IMappingSpan" />.
        /// </summary>
        /// <param name="span">The <see cref="IMappingSpan" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> is null.</exception>
        public TagsChangedEventArgs(IMappingSpan span)
        {
            if (span == null)
                throw new ArgumentNullException(nameof(span));

            Span = span;
        }
    }
}
