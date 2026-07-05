//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Associates an <see cref="ITag" /> with a specified <see cref="IMappingSpan" />.
    /// </summary>
    /// <typeparam name="T">The type, which must be a subtype of <see cref="ITag"/>.</typeparam>
    /// <remarks>
    /// Use <see cref="MappingTagSpan&lt;T&gt;" /> as the implementation of this
    /// interface.
    /// </remarks>
    public interface IMappingTagSpan<out T> where T : ITag
    {
        /// <summary>
        /// Gets the tag located in this span.
        /// </summary>
        T Tag { get; }

        /// <summary>
        /// Gets the mapping span for this tag.
        /// </summary>
        IMappingSpan Span { get; }
    }

    /// <summary>
    /// The implementation of IMappingTagSpan&lt;T&gt;.
    /// </summary>
    public class MappingTagSpan<T> : IMappingTagSpan<T> where T : ITag
    {
        #region IMappingTagSpan<T> members

        /// <summary>
        /// Gets the tag located in this span.
        /// </summary>
        public T Tag { get; private set; }

        /// <summary>
        /// Gets the mapping span for this tag.
        /// </summary>
        public IMappingSpan Span { get; private set; }

        #endregion

        /// <summary>
        /// Creates a mapping tag span.
        /// </summary>
        /// <param name="span">The mapping span with which to associate the tag.</param>
        /// <param name="tag">The tag associated with the span.</param>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> or <paramref name="tag"/> is null.</exception>
        public MappingTagSpan(IMappingSpan span, T tag)
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
