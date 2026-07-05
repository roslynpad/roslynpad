//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    // TODO: Replace the logic in this class with the upcoming Line/Word
    // split utility. 
    
    /// <summary>
    /// This is a word decomposition of the given string.
    /// </summary>
    internal sealed class WordDecompositionList : TokenizedStringList
    {
        public WordDecompositionList(string original, StringDifferenceOptions options) 
            : base(original)
        {
            this.CreateTokens(options, ignoreTrimWhiteSpace: false);    // We never paid attention to trim whitespace for strings when doing line or word diffs.
        }

        public WordDecompositionList(SnapshotSpan original, StringDifferenceOptions options)
            : base(original)
        {
            this.CreateTokens(options, options.IgnoreTrimWhiteSpace);
        }

        private void CreateTokens(StringDifferenceOptions options, bool ignoreTrimWhiteSpace)
        {
            int end = this.OriginalLength;
            int i = 0;
            int tokenStart = 0;
            TokenType previousTokenType = TokenType.WhiteSpace;
            bool skipPreviousToken = ignoreTrimWhiteSpace;     // Assume that the 1st whitespace token is ignoreable if we're trimming whitespace.
            while (i < end)
            {
                bool skipNextToken;
                TokenType nextTokenType;
                int breakLength = this.LengthOfLineBreak(i, end);
                if (breakLength != 0)
                {
                    nextTokenType = ignoreTrimWhiteSpace ? TokenType.WhiteSpace : TokenType.LineBreak;
                    skipNextToken = ignoreTrimWhiteSpace;
                }
                else
                {
                    nextTokenType = GetTokenType(this.CharacterAt(i), options.WordSplitBehavior);
                    skipNextToken = (nextTokenType == TokenType.WhiteSpace) ? skipPreviousToken : false;
                    breakLength = 1;
                }

                if ((nextTokenType != previousTokenType) || (nextTokenType == TokenType.Symbol))
                {
                    if ((tokenStart < i) && !skipPreviousToken)
                    {
                        this.Tokens.Add(new Span(tokenStart, i - tokenStart));
                    }

                    previousTokenType = nextTokenType;
                    tokenStart = i;
                }

                skipPreviousToken = skipNextToken;
                i += breakLength;
            }

            if ((end == 0) ||                                                           // 0-length sequences get a single token
                !(ignoreTrimWhiteSpace && (previousTokenType == TokenType.WhiteSpace))) // act as if there is an implicit line break at the end of the line.
            {
                this.Tokens.Add(new Span(tokenStart, end - tokenStart));
            }
        }

        private enum TokenType
        {
            LineBreak,
            WhiteSpace,
            Symbol,
            Digit,
            Letter,
            Other,
        };

        private static TokenType GetTokenType(char c, WordSplitBehavior splitBehavior)
        {
            if (char.IsWhiteSpace(c))
                return TokenType.WhiteSpace;

            if (splitBehavior == WordSplitBehavior.WhiteSpace)
                return TokenType.Other;

            if (char.IsPunctuation(c) || char.IsSymbol(c))
                return TokenType.Symbol;

            if (splitBehavior == WordSplitBehavior.WhiteSpaceAndPunctuation)
                return TokenType.Other;

            if (char.IsDigit(c) || char.IsNumber(c))
                return TokenType.Digit;

            if (char.IsLetter(c))
                return TokenType.Letter;

            return TokenType.Other;
        }

        // This is roughly a copy of TextUtilities.LengthOfLineBreak (but using CharacterAt).
        public int LengthOfLineBreak(int start, int end)
        {
            char c1 = this.CharacterAt(start);
            if (c1 == '\r')
            {
                return ((++start < end) && (this.CharacterAt(start) == '\n')) ? 2 : 1;
            }
            else if ((c1 == '\n') || (c1 == '\u0085') ||
                     (c1 == '\u2028' /*unicode line separator*/) ||
                     (c1 == '\u2029' /*unicode paragraph separator*/))
            {
                return 1;
            }
            else
                return 0;
        }
    }
}
