//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion
{
    using System;
    using System.Composition;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Specifies the opening and closing braces.
    /// <remarks>
    /// This attribute may be exported on an <see cref="IBraceCompletionSessionProvider"/>, <see cref="IBraceCompletionContextProvider"/>,
    /// or <see cref="IBraceCompletionDefaultProvider"/>.
    /// </remarks></summary>
    [MetadataAttribute]
    [System.ComponentModel.Composition.MetadataAttribute] // for MEF v1 parts composed via VS-MEF
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class BracePairAttribute : MultipleBaseMetadataAttribute
    {
        private readonly char openingBraces;
        private readonly char closingBraces;

        /// <summary>
        /// Instantiates a new instance of a <see cref="BracePairAttribute"/>.
        /// </summary>
        /// <param name="openingBrace">The opening brace character for this brace completion session.</param>
        /// <param name="closingBrace">The closing brace character for this brace completion session.</param>
        public BracePairAttribute(char openingBrace, char closingBrace)
        {
            openingBraces = openingBrace;
            closingBraces = closingBrace;
        }

        /// <summary>
        /// The opening brace character.
        /// </summary>
        public char OpeningBraces
        {
            get
            {
                return openingBraces;
            }
        }

        /// <summary>
        /// The closing brace character.
        /// </summary>
        public char ClosingBraces
        {
            get
            {
                return closingBraces;
            }
        }
    }
}
