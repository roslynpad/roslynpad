//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// Behavior to use while splitting words in string differencing.
    /// </summary>
    public enum WordSplitBehavior
    {
        /// <summary>
        /// Split words by <see cref="CharacterClass" />.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Split words by character class.
        /// </summary>
        /// <remarks>
        /// The word split logic uses the following character classes:
        /// <list type="number">
        /// <item><description>Whitespace and control characters</description></item>
        /// <item><description>Numbers/Digits</description></item>
        /// <item><description>Punctuation/Symbols</description></item>
        /// <item><description>Letters</description></item>
        /// </list>
        /// </remarks>
        CharacterClass = 0,

        /// <summary>
        /// Split the text into words by whitespace only.
        /// </summary>
        WhiteSpace,

        /// <summary>
        /// Split the text into words by whitespace and punctuation/symbols.
        /// </summary>
        WhiteSpaceAndPunctuation,

        /// <summary>
        /// Split the text into language-appropriate words.
        /// </summary>
        /// <remarks>
        /// When used in conjunction with the default <see cref="IHierarchicalStringDifferenceService"/>,
        /// this is the same as <see cref="WhiteSpaceAndPunctuation"/>.
        /// When used in conjunction with an <see cref="ITextDifferencingService"/>, the behavior
        /// is controlled by the implementation.
        /// </remarks>
        LanguageAppropriate
    }
}
