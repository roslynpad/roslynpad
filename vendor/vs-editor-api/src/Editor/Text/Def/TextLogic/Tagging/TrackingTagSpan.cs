//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Associates an <see cref="ITag" /> with a given <see cref="ITrackingSpan" />.
    /// This is used by SimpleTagger to provide buffer-level tracking and caching of tag spans.
    /// </summary>
    /// <typeparam name="T">The type, which must be a subclass of <see cref="ITag"/>.</typeparam>
    public class TrackingTagSpan<T> where T : ITag
    {
        /// <summary>
        /// The tag located in this span.
        /// </summary>
        public T Tag { get; private set; }

        /// <summary>
        /// The tracking span for this tag.
        /// </summary>
        public ITrackingSpan Span { get; private set; }

        /// <summary>
        /// Initializes a new instance of a <see cref="TrackingTagSpan&lt;T&gt;"/>.
        /// </summary>
        /// <param name="span">The tracking span with which to associate the tag.</param>
        /// <param name="tag">The tag associated with the span.</param>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> or <paramref name="tag"/> is null.</exception>
        public TrackingTagSpan(ITrackingSpan span, T tag)
        {
            if (span == null)
                throw new ArgumentNullException(nameof(span));
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            Span = span;
            Tag = tag;
        }
    }
}
