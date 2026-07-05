//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A text buffer that contains projections of other text buffers, composed
    /// of a list of tracking spans of those buffers. The buffers that contribute to
    /// the projection buffer are called source buffers, and the tracking spans that describe
    /// the contributed regions are called source spans.
    /// </summary>
    public interface IProjectionBuffer : IProjectionBufferBase
    {
        #region Span Editing
        /// <summary>
        /// Inserts a tracking span into the list of source spans. 
        /// </summary>
        /// <param name="position">The position at which to insert <paramref name="spanToInsert"/>.</param>
        /// <param name="spanToInsert">The span to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than SpanCount.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="spanToInsert"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="spanToInsert"/> would cause any duplicated projection.</exception>
        /// <exception cref="ArgumentException"><paramref name="spanToInsert"/> is EdgeInclusive and does not cover its entire buffer,
        /// or is EdgePositive and does not abut the end of its buffer, or is EdgeNegative and does not abut the beginning of its
        /// buffer. 
        /// These checks are not performed if the projection buffer was created with the PermissiveEdgeInclusiveSourceSpans option.</exception>
        /// <exception cref="ArgumentException">Adding the TextBuffer containing <paramref name="spanToInsert"/> would create a cycle
        /// among a set of projection buffers by virtue of the SourceBuffer relationship.</exception>
        IProjectionSnapshot InsertSpan(int position, ITrackingSpan spanToInsert);

        /// <summary>
        /// Inserts a literal string into the list of SourceSpans. 
        /// </summary>
        /// <param name="position">The position at which to insert <paramref name="literalSpanToInsert"/>.</param>
        /// <param name="literalSpanToInsert">The string to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than SpanCount.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="literalSpanToInsert"/> is null.
        /// These checks are not performed if the projection buffer was created with the PermissiveEdgeInclusiveSourceSpans option.</exception>
        IProjectionSnapshot InsertSpan(int position, string literalSpanToInsert);

        /// <summary>
        /// Inserts a list of <see cref="ITrackingSpan"/> objects and/or literal strings into the list of source spans in the order in which they appear in the list. 
        /// </summary>
        /// <param name="position">The position at which to insert the spans.</param>
        /// <param name="spansToInsert">The list of spans to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than SpanCount.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="spansToInsert"/> is null or a span in that list is null.</exception>
        /// <exception cref="ArgumentException">An element of <parmref name="spansToInsert"/> is neither an <see cref="ITrackingSpan"/> nor a string.</exception>
        /// <exception cref="ArgumentException">A span in <paramref name="spansToInsert"/> would cause a duplicated projection.</exception>
        /// <exception cref="ArgumentException">A tracking span in <paramref name="spansToInsert"/> is EdgeInclusive and does not cover its entire buffer,
        /// or is EdgePositive and does not abut the end of its buffer, or is EdgeNegative and does not abut the beginning of its
        /// buffer.
        /// These checks are not performed if the projection buffer was created with the PermissiveEdgeInclusiveSourceSpans option.</exception>
        /// <exception cref="ArgumentException">Adding one of the text buffers containing any of the <paramref name="spansToInsert"/> would 
        /// create a cycle among a set of projection vuffers by virtue of the SourceBuffer relationship.</exception>
        IProjectionSnapshot InsertSpans(int position, IList<object> spansToInsert);

        /// <summary>
        /// Deletes a sequence of source spans from the projection buffer.
        /// </summary>
        /// <param name="position">The position at which to begin deleting spans.</param>
        /// <param name="spansToDelete">The number of spans to delete.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than SpanCount.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="spansToDelete"/> is less than zero or 
        /// <paramref name="position"/> + <paramref name="spansToDelete"/> is greater than SpanCount.</exception>
        IProjectionSnapshot DeleteSpans(int position, int spansToDelete);

        /// <summary>
        /// Replaces a sequence of source spans with a new list of <see cref="ITrackingSpan"/> objects and/or literal strings.
        /// </summary>
        /// <param name="position">The position at which to begin replacing spans.</param>
        /// <param name="spansToReplace">The number of spans to replace.</param>
        /// <param name="spansToInsert">The new spans to insert.</param>
        /// <param name="options">Options to apply to the span edit.</param>
        /// <param name="editTag">An arbitrary object that will be associated with this edit transaction.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than SpanCount.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="spansToReplace"/> is less than zero or <paramref name="position"/> + <paramref name="spansToReplace"/>
        /// is greater than SpanCount.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="spansToInsert"/> is null or a span in the list are null.</exception>
        /// <exception cref="ArgumentException">An element of <parmref name="spansToInsert"/> is neither an <see cref="ITrackingSpan"/> nor a string.</exception>
        /// <exception cref="ArgumentException">A span in <paramref name="spansToInsert"/> would cause a duplicated projection.</exception>
        /// <exception cref="ArgumentException">A tracking span in <paramref name="spansToInsert"/> is EdgeInclusive and does not cover its entire buffer,
        /// or is EdgePositive and does not abut the end of its buffer, or is EdgeNegative and does not abut the beginning of its
        /// buffer.
        /// These checks are not performed if the projection buffer was created with the PermissiveEdgeInclusiveSourceSpans option.</exception>
        /// <exception cref="ArgumentException">Adding a text buffer containing one of the <paramref name="spansToInsert"/> would 
        /// create a cycle among a set of projection buffers by virtue of the SourceBuffer relationship.</exception>
        IProjectionSnapshot ReplaceSpans(int position, int spansToReplace, IList<object> spansToInsert, EditOptions options, object editTag);
        #endregion

        /// <summary>
        /// Raised when source spans are added or deleted. It is not raised when
        /// the contents of a source span change, for example when a source span becomes empty. When
        /// a nonempty span is added or deleted, the <see cref="SourceBuffersChanged"/> event will be raised first. 
        /// The sequence of events is: 1) SourceBuffersChanged, 2) SourceSpansChanged, 3) ITextBuffer.Changed.
        /// The <see cref="SourceSpansChanged"/> event is raised first).
        /// </summary>
        event EventHandler<ProjectionSourceSpansChangedEventArgs> SourceSpansChanged;

        /// <summary>
        /// Raised when source buffers are added or deleted by virtue of the addition or deletion
        /// of source spans. This event is raised before the <see cref="SourceSpansChanged"/> event is raised.
        /// </summary>
        event EventHandler<ProjectionSourceBuffersChangedEventArgs> SourceBuffersChanged;
    }
}
