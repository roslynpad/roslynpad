//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// A handle that tracks a possibly empty read-only region of text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The text in a read-only region is not necessarily immutable; a read-only region created on a 
    /// projection buffer makes that region read-only to clients of the projection buffer but
    /// does not affect the source buffers for that text. </para>
    /// <para>
    /// Read-only regions prevent edits only on their owning buffer.
    /// A read-only region that does not prohibit edge insertions does not prevent any insertion if the
    /// region has (or shrinks to) zero length.
    /// A zero-length read-only region that prohibits edge insertions prevents insertions only at its starting
    /// position, but allows deletions and modifications that span that position.
    /// </para>
    /// </remarks>
    public interface IReadOnlyRegion
    {
        /// <summary>
        /// The edge insertion behavior of the read-only region.
        /// </summary>
        EdgeInsertionMode EdgeInsertionMode { get; }

        /// <summary>
        /// The span of text marked read-only by this region.
        /// </summary>
        /// <remarks>
        /// Not null.
        /// </remarks>
        ITrackingSpan Span { get; }

        /// <summary>
        /// The delegate that notifies the read-only region of read-only checks and edits.
        /// </summary>
        /// <remarks>
        /// <para>May be null.</para>
        /// </remarks>
        DynamicReadOnlyRegionQuery QueryCallback { get; }
    }
}
