//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Represents tracking modes for <see cref="ITrackingSpan"/> objects.
    /// </summary>
    public enum SpanTrackingMode
    {
        /// <summary>
        /// The leading edge of the span is positive tracking (insertions push the current position towards the end) 
        /// and the trailing edge is negative tracking (insertions push the current position towards the start).
        /// The span will not expand when text changes occur at the span boundaries. For example,
        /// if an EdgeExclusive Span has Start position 3, and a single character is inserted at position 3,
        /// the Span will then have Start position 4 and its length will be unchanged.
        /// </summary>
        EdgeExclusive,

        /// <summary>
        /// The leading edge of the span is negative tracking (insertions push the current position toward the start) 
        /// and the trailing edge is positive tracking (insertions push the current position toward the end).
        /// The span will expand when text changes occur at the span boundaries. For example,
        /// if an EdgeInclusive Span has Start position 3, and a single character is inserted at position 3,
        /// the Span will then have Start position 3 and its length will be increased by one.
        /// </summary>
        EdgeInclusive,

        /// <summary>
        /// Both edges of the span are positive tracking (insertions push the current position toward the end).
        /// </summary>
        EdgePositive,

        /// <summary>
        /// Both edges of the span are negative tracking (insertions push the current position toward the start).
        /// </summary>
        EdgeNegative,

        /// <summary>
        /// Custom client-determined tracking behavior.
        /// </summary>
        Custom
    }
}