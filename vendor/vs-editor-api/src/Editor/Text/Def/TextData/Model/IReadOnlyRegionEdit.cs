//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Provides for an atomic set of read-only region editing operations on an <see cref="ITextBuffer"/>. 
    /// Edition positions are specified with respect to the state of the <see cref="ITextBuffer"/> 
    /// at the time the <see cref="IReadOnlyRegionEdit"/> object was created.
    /// </summary>
    /// <remarks>
    /// <para>At most one <see cref="ITextBufferEdit"/> object may be active for a particular <see cref="ITextBuffer"/>. It is considered
    /// active as long as it has been neither applied nor canceled; calling Dispose() on an unapplied object is equivalent to calling Cancel. </para>
    /// <para>The operations performed using this object do not appear in the <see cref="ITextBuffer"/> until the <see cref="ITextBufferEdit.Apply"/> 
    /// method has been called.</para>
    /// </remarks>
    public interface IReadOnlyRegionEdit : ITextBufferEdit
    {
        /// <summary>
        /// Marks a span of text in this buffer as read-only. The span remains 
        /// read-only until the <see cref="IReadOnlyRegion"/> is removed.
        /// </summary>
        /// <param name="span">
        /// The span to mark as read-only.
        /// </param>
        /// <returns>
        /// The <see cref="IReadOnlyRegion"/> used to track this read-only region. This object must be used
        /// to remove the read-only region.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/> is past the end of the buffer.</exception>
        /// <remarks>Insertions at the edge of the read-only region are allowed by default.</remarks>
        /// <remarks>The region is created edge exclusive by default.</remarks>
        IReadOnlyRegion CreateReadOnlyRegion(Span span);

        /// <summary>
        /// Marks a span of text in this buffer as read-only.  The span remains 
        /// read-only until it is marked as writable or forced writable again.
        /// </summary>
        /// <param name="span">
        /// The span to mark as read-only.
        /// </param>
        /// <param name="trackingMode">
        /// Specifies the tracking behavior of the read-only region.
        /// </param>
        /// <param name="edgeInsertionMode">
        /// Specifies the edge insertion behavior of the read-only region.
        /// </param>
        /// <returns>
        /// The <see cref="IReadOnlyRegion"/> used to track this read-only region. This object will be used
        /// to remove the read-only region.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/> is past the end of the buffer.</exception>
        /// <remarks>
        /// Zero-length read-only regions restrict inserts only at that point.  A deletion or modification over that span
        /// can still occur.
        /// </remarks>
        IReadOnlyRegion CreateReadOnlyRegion(Span span, SpanTrackingMode trackingMode, EdgeInsertionMode edgeInsertionMode);

        /// <summary>
        /// Marks a span of text in this buffer as as conditionally read-only,
        /// subject to a check performed when the region is queried.  The span remains 
        /// read-only until it is marked as writable or forced writable again.
        /// </summary>
        /// <param name="span">
        /// The span to mark as read-only.
        /// </param>
        /// <param name="trackingMode">
        /// Specifies the tracking behavior of the read-only region.
        /// </param>
        /// <param name="edgeInsertionMode">
        /// Specifies the edge insertion behavior of the read-only region.
        /// </param>
        /// <param name="callback">
        /// The delegate that notifies the read-only region of read-only checks and edits. May be null.
        /// See <see cref="IReadOnlyRegion.QueryCallback"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IReadOnlyRegion"/> used to track this read-only region. This object will be used
        /// to remove the read-only region.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="span"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/> is past the end of the buffer.</exception>
        /// <remarks>
        /// Zero-length read-only regions restrict inserts only at that point.  A deletion or modification over that span
        /// can still occur.
        /// </remarks>
        IReadOnlyRegion CreateDynamicReadOnlyRegion(Span span, SpanTrackingMode trackingMode, EdgeInsertionMode edgeInsertionMode, DynamicReadOnlyRegionQuery callback);

        /// <summary>
        /// Removes the read-only region from the list of read-only regions in this buffer.
        /// </summary>
        /// <param name="readOnlyRegion">
        /// The read-only region to remove.
        /// </param>
        /// <remarks>
        /// Removing a read-only region that has already been removed does nothing.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="readOnlyRegion"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="readOnlyRegion"/> was created on another buffer.</exception>
        void RemoveReadOnlyRegion(IReadOnlyRegion readOnlyRegion);
    }
}
