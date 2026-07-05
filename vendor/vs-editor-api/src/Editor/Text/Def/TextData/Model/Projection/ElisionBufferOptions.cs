//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;

    /// <summary>
    /// Options that apply to an <see cref="IElisionBuffer"/>s.
    /// </summary>
    [Flags]
    public enum ElisionBufferOptions
    {
        /// <summary>
        /// No special treatment.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// When mapping spans, include hidden text between the start point and the end point.
        /// </summary>
        FillInMappingMode = 0x01
    }
}
