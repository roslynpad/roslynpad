// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal class InterpolationCompletionSession : IEditorBraceCompletionSession
    {
        public char OpeningBrace => Braces.CurlyBrace.OpenCharacter;

        public char ClosingBrace => Braces.CurlyBrace.CloseCharacter;

        public InterpolationCompletionSession()
        {
        }

        public void AfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
        }

        public void AfterStart(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
        }

        public bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            return true;
        }

        public bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var position = session.OpeningPoint;
            var token = session.Document.FindToken(position, cancellationToken);

            return token.IsKind(SyntaxKind.OpenBraceToken)
                && token.SpanStart == position;
        }

        public static bool IsContext(Document document, int position, CancellationToken cancellationToken)
        {
            // First, check to see if the character to the left of the position is an open curly. If it is,
            // we shouldn't complete because the user may be trying to escape a curly.
            var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var index = position - 1;
            var openCurlyCount = 0;
            while (index >= 0)
            {
                if (text[index] == Braces.CurlyBrace.OpenCharacter)
                {
                    openCurlyCount++;
                }
                else
                {
                    break;
                }

                index--;
            }

            if (openCurlyCount > 0 && openCurlyCount % 2 == 1)
            {
                return false;
            }

            // Next, check to see if we're typing in an interpolated string
            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var token = root.FindTokenOnLeftOfPosition(position);

            if (!token.Span.IntersectsWith(position))
            {
                return false;
            }

            return token.IsKind(
                SyntaxKind.InterpolatedStringStartToken,
                SyntaxKind.InterpolatedVerbatimStringStartToken,
                SyntaxKind.InterpolatedStringTextToken,
                SyntaxKind.InterpolatedStringEndToken);
        }
    }
}
