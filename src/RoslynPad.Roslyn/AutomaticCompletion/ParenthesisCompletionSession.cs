// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal class ParenthesisCompletionSession : AbstractTokenBraceCompletionSession
    {
        public ParenthesisCompletionSession(ISyntaxFactsService syntaxFactsService)
            : base(syntaxFactsService, (int)SyntaxKind.OpenParenToken, (int)SyntaxKind.CloseParenToken,
                Braces.Parenthesis.OpenCharacter, Braces.Parenthesis.CloseCharacter)
        {
        }

        public override bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var document = session.Document;
            var position = session.OpeningPoint;
            var token = document.FindToken(position, cancellationToken);

            // check token at the opening point first
            if (!IsValidToken(token) ||
                token.RawKind != OpeningTokenKind ||
                token.SpanStart != position || token.Parent == null)
            {
                return false;
            }

            // now check whether parser think whether there is already counterpart closing parenthesis
            var pair = token.Parent.GetParentheses();

            // if pair is on the same line, then the closing parenthesis must belong to other tracker.
            // let it through
            var text = session.Text;
            if (text.Lines.GetLinePosition(pair.openBrace.SpanStart).Line == text.Lines.GetLinePosition(pair.closeBrace.Span.End).Line)
            {
                return true;
            }

            return (int)pair.closeBrace.Kind() != ClosingTokenKind || pair.closeBrace.Span.Length == 0;
        }
    }
}
