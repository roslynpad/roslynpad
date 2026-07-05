//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Used to create projection buffers.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IProjectionBufferFactoryService factory = null;
    /// </remarks>
    public interface IProjectionBufferFactoryService
    {
        /// <summary>
        /// The default content type for projection buffers.
        /// </summary>
        IContentType ProjectionContentType { get; }

        /// <summary>
        /// Creates an <see cref="IProjectionBuffer"/> object with a specified <see cref="IContentType"/> and
        /// the specified list of <see cref="ITrackingSpan"/> objects and/or literal strings.
        /// </summary>
        /// <param name="projectionEditResolver">The conflict resolver for this <see cref="IProjectionBuffer"/>. May be null.</param>
        /// <param name="sourceSpans">The initial set of source spans for the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="options">Options for this buffer.</param>
        /// <param name="contentType">The <see cref="IContentType"/> for the new <see cref="IProjectionBuffer"/>.</param>
        /// <returns>A non-null projection buffer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sourceSpans"/> or any of its elements is null.</exception>
        /// <exception cref="ArgumentException">An element of <paramref name="sourceSpans"/> is neither a string nor an <see cref="ITrackingSpan"/>.</exception>
        /// <exception cref="ArgumentException">A tracking span in <paramref name="sourceSpans"/> is EdgeInclusive and does not cover its entire buffer,
        /// or is EdgePositive and does not abut the end of its buffer, or is EdgeNegative and does not abut the beginning of its
        /// buffer.
        /// These checks are not performed if the projection buffer was created with the PermissiveEdgeInclusiveSourceSpans option.</exception>
        /// <exception cref="ArgumentException">Some of the tracking spans in <paramref name="sourceSpans"/> overlap.</exception>
        IProjectionBuffer CreateProjectionBuffer(IProjectionEditResolver projectionEditResolver,
                                                 IList<object> sourceSpans,
                                                 ProjectionBufferOptions options,
                                                 IContentType contentType);

        /// <summary>
        /// Creates an <see cref="IProjectionBuffer"/> object with the default projection <see cref="IContentType"/> and  
        /// the specified list of source spans.
        /// </summary>
        /// <param name="projectionEditResolver">The conflict resolver for this <see cref="IProjectionBuffer"/>. May be null.</param>
        /// <param name="sourceSpans">The initial set of source spans for the <see cref="IProjectionBuffer"/>.</param>
        /// <param name="options">Options for this buffer.</param>
        /// <returns>A non-null projection buffer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sourceSpans"/> or any of its elements is null.</exception>
        /// <exception cref="ArgumentException">An element of <paramref name="sourceSpans"/> is neither a string nor an <see cref="ITrackingSpan"/>.</exception>
        /// <exception cref="ArgumentException">A tracking spans in <paramref name="sourceSpans"/> is EdgeInclusive and does not cover its entire buffer,
        /// or is EdgePositive and does not abut the end of its buffer, or is EdgeNegative and does not abut the beginning of its
        /// buffer.</exception>
        /// <exception cref="ArgumentException">Any of the tracking spans in <paramref name="sourceSpans"/> overlap.</exception>
        IProjectionBuffer CreateProjectionBuffer(IProjectionEditResolver projectionEditResolver,
                                                 IList<object> sourceSpans,
                                                 ProjectionBufferOptions options);

        /// <summary>
        /// Create an elision buffer initialized to expose the provided list of snapshot spans from a single source buffer.
        /// </summary>
        /// <param name="projectionEditResolver">The conflict resolver for this <see cref="IProjectionBuffer"/>. May be null.</param>
        /// <param name="exposedSpans">The set of spans (from a single source buffer) that are initially exposed in the elision buffer.</param>
        /// <param name="options">Options for this buffer.</param>
        /// <param name="contentType">The <see cref="IContentType"/> for the new <see cref="IElisionBuffer"/>.</param>
        /// <returns>A non-null elision buffer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="contentType"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="exposedSpans"/> is null.</exception>
        IElisionBuffer CreateElisionBuffer(IProjectionEditResolver projectionEditResolver,
                                           NormalizedSnapshotSpanCollection exposedSpans,
                                           ElisionBufferOptions options,
                                           IContentType contentType);

        /// <summary>
        /// Create an ElisionBuffer with the default projection <see cref="IContentType"/> and initialized to the provided list of snapshot spans from
        /// a single source buffer.
        /// </summary>
        /// <param name="projectionEditResolver">The conflict resolver for this <see cref="IProjectionBuffer"/>. May be null.</param>
        /// <param name="exposedSpans">The set of spans (from a single source buffer) that are initially exposed in the elision buffer.</param>
        /// <param name="options">Options for this buffer.</param>
        /// <returns>A non-null elision buffer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exposedSpans"/> is null.</exception>
        IElisionBuffer CreateElisionBuffer(IProjectionEditResolver projectionEditResolver,
                                           NormalizedSnapshotSpanCollection exposedSpans,
                                           ElisionBufferOptions options);

        /// <summary>
        /// Raised when any <see cref="IProjectionBuffer"/> or <see cref="IElisionBuffer"/> is created.
        /// </summary>
        event EventHandler<TextBufferCreatedEventArgs> ProjectionBufferCreated;
    }
}
