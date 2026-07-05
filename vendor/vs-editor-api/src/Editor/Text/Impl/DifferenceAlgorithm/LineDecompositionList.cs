//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    // TODO: Replace the logic in this class with the upcoming Line/Word
    // split utility. 
    
    /// <summary>
    /// This is a decomposition of the given string into lines.
    /// </summary>
    internal sealed class LineDecompositionList : TokenizedStringList
    {
        public LineDecompositionList(string original, bool ignoreTrimWhiteSpace)
            : base(original)
        {
            if (original.Length == 0)
            {
                base.Tokens.Add(new Span(0, 0));
            }
            else
            {
                int firstNonWhitespace = -1;
                int lastNonWhitespace = -1;
                int start = 0;
                int i = 0;
                while (i < original.Length)
                {
                    int breakLength = TextUtilities.LengthOfLineBreak(original, i, original.Length);
                    if (breakLength > 0)
                    {
                        i += breakLength;
                        if (ignoreTrimWhiteSpace)
                        {
                            base.Tokens.Add((firstNonWhitespace == -1)
                                            ? new Span(i - breakLength, 0)
                                            : Span.FromBounds(firstNonWhitespace, lastNonWhitespace + 1));
                        }
                        else
                        {
                            base.Tokens.Add(Span.FromBounds(start, i));
                        }

                        start = i;

                        firstNonWhitespace = -1;
                        lastNonWhitespace = -1;
                    }
                    else
                    {
                        if (!char.IsWhiteSpace(original[i]))
                        {
                            if (firstNonWhitespace == -1)
                            {
                                firstNonWhitespace = i;
                            }
                            lastNonWhitespace = i;
                        }

                        ++i;
                    }
                }

                if (ignoreTrimWhiteSpace)
                {
                    base.Tokens.Add((firstNonWhitespace == -1)
                                    ? new Span(original.Length, 0)
                                    : Span.FromBounds(firstNonWhitespace, lastNonWhitespace + 1));
                }
                else
                {
                    base.Tokens.Add(Span.FromBounds(start, original.Length));
                }
            }
        }
    }
}
