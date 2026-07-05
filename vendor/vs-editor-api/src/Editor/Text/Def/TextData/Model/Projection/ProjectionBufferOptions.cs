//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;

    /// <summary>
    /// Represents the options that apply to <see cref="IProjectionBuffer"/> objects.
    /// </summary>
    [Flags]
    public enum ProjectionBufferOptions
    {
        /// <summary>
        /// No special treatment.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Do not perform certain consistency checks on edge-inclusive source spans. 
        /// </summary>
        /// <remarks>
        /// See <see cref="IProjectionBuffer"/> for details.
        /// </remarks>
        PermissiveEdgeInclusiveSourceSpans = 0x01,

        /// <summary>
        /// Allow source spans that are string literals to be edited.
        /// </summary>
        WritableLiteralSpans = 0x02
    }
}
