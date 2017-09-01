// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal class InterpolatedStringCompletionSession : IEditorBraceCompletionSession
    {
        public char OpeningBrace => Braces.DoubleQuote.OpenCharacter;

        public char ClosingBrace => Braces.DoubleQuote.CloseCharacter;

        public InterpolatedStringCompletionSession()
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
            var document = session.Document;
            var position = session.OpeningPoint;
            var token = document.FindToken(position, cancellationToken);

            return token.IsKind(SyntaxKind.InterpolatedStringStartToken, SyntaxKind.InterpolatedVerbatimStringStartToken)
                && token.Span.End - 1 == position;
        }

        public static bool IsContext(Document document, int position, CancellationToken cancellationToken)
        {
            // Check to see if we're to the right of an $ or an @$
            var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var start = position - 1;
            if (start < 0)
            {
                return false;
            }

            if (text[start] == '@')
            {
                start--;

                if (start < 0)
                {
                    return false;
                }
            }

            if (text[start] != '$')
            {
                return false;
            }

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var token = root.FindTokenOnLeftOfPosition(start);

            return root.SyntaxTree.IsExpressionContext(start, token, attributes: false, cancellationToken: cancellationToken)
                || root.SyntaxTree.IsStatementContext(start, token, cancellationToken);
        }
    }
}
