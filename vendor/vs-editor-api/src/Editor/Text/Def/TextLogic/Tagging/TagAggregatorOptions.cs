//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using Microsoft.VisualStudio.Text.Projection;

    /// <summary>
    /// Tag Aggregator options.
    /// </summary>
    [Flags]
    public enum TagAggregatorOptions
    {
        /// <summary>
        /// Default behavior. The tag aggregator will map up and down through all projection buffers.
        /// </summary>
        None = 0,

        /// <summary>
        /// Only map through projection buffers that have the "projection" content type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Normally, a tag aggregator will map up and down through all projection buffers (buffers
        /// that implement <see cref="IProjectionBufferBase"/>).  This flag will cause the projection buffer
        /// to not map through buffers that are projection buffers but do not have a projection content type.
        /// </para>
        /// </remarks>
        /// <comment>This is used by the classifier aggregator, as classification depends on content type.</comment>
        MapByContentType = 0x1
    }
}