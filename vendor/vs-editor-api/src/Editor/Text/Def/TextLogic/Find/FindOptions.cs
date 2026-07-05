//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Represents the options that are used in a search.
    /// </summary>
    [Flags]
    public enum FindOptions
    {
        /// <summary>
        /// No options have been set.
        /// </summary>
        None = 0x000,

        /// <summary>
        /// The search is case-sensitive.
        /// </summary>
        MatchCase = 0x01,

        /// <summary>
        /// The search uses .NET regular expressions.
        /// </summary>
        UseRegularExpressions = 0x02,

        /// <summary>
        /// The search matches whole words only.
        /// </summary>
        WholeWord = 0x04,

        /// <summary>
        /// The search starts at the end of the string.
        /// </summary>
        SearchReverse = 0x08,

        /// <summary>
        /// The search should wrap around if it hits boundaries of the search range.
        /// </summary>
        Wrap = 0x10,

        /// <summary>
        /// The search contains data that could match over line endings.
        /// </summary>
        Multiline = 0x20,

        /// <summary>
        /// The string comparison used for the search is culture-insensitive (ordinal). For regular expression searches,
        /// this options specifies the <see cref="RegexOptions.CultureInvariant"/>.
        /// </summary>
        OrdinalComparison = 0x40,

        /// <summary>
        /// Only valid in conjunction with <see cref="UseRegularExpressions"/>. When supplied, uses the <see cref="RegexOptions.Singleline"/> option to perform the searches.
        /// </summary>
        SingleLine = 0x80
    }
}
