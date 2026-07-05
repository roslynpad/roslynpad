//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    public enum IgnoreWhiteSpaceBehavior
    {
        /// <summary>
        /// Don't ignore whitespace.
        /// </summary>
        None,

        /// <summary>
        /// Ignore whitespace at the start and end of lines when performing line-level differencing.
        /// </summary>
        /// <remarks>This is equivalent to <see cref="StringDifferenceOptions.IgnoreTrimWhiteSpace"/>.</remarks>
        IgnoreTrimWhiteSpace,

        /// <summary>
        /// Ignore all whitespace when performing line-level differencing.
        /// </summary>
        IgnoreAllWhiteSpace
    }
}
