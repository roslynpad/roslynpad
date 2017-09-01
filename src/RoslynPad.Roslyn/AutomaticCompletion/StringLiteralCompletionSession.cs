// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServices;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal class StringLiteralCompletionSession : AbstractTokenBraceCompletionSession
    {
        private const char VerbatimStringPrefix = '@';

        public StringLiteralCompletionSession(ISyntaxFactsService syntaxFactsService)
            : base(syntaxFactsService, (int)SyntaxKind.StringLiteralToken, (int)SyntaxKind.StringLiteralToken,
                Braces.DoubleQuote.OpenCharacter, Braces.DoubleQuote.CloseCharacter)
        {
        }

        public override bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var document = session.Document;
            var text = session.Text;

            var position = session.OpeningPoint;
            var token = document.FindToken(position, cancellationToken);

            if (!IsValidToken(token) || token.RawKind != OpeningTokenKind)
            {
                return false;
            }

            if (token.SpanStart == position)
            {
                return true;
            }

            return token.SpanStart + 1 == position && text[token.SpanStart] == VerbatimStringPrefix;
        }

        public override bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            return true;
        }
    }
}
