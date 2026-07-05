//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A bitwise combination of the enumeration values to use when computing differences with the various methods in 
    /// <see cref="IHierarchicalStringDifferenceService" />. 
    /// </summary>
    /// <remarks>
    /// See the comments on 
    /// <see cref="IHierarchicalStringDifferenceService" /> for an explanation of how differences are computed.
    /// Since computing differences can be slow with large data sets, you should not use the Character type
    /// unless the given text is relatively small.
    /// </remarks>
    [Flags]
    public enum StringDifferenceTypes
    {
        /// <summary>
        /// Compute the line difference.
        /// </summary>
        Line = 1,

        /// <summary>
        /// Compute the word difference.
        /// </summary>
        Word = 2,

        /// <summary>
        /// Compute the character difference.
        /// </summary>
        Character = 4
    }
}
