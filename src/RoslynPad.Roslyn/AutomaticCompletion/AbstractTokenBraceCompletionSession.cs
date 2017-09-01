// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal abstract class AbstractTokenBraceCompletionSession : IEditorBraceCompletionSession
    {
        private readonly ISyntaxFactsService _syntaxFactsService;

        protected int OpeningTokenKind { get; }
        protected int ClosingTokenKind { get; }
        public char OpeningBrace { get; }
        public char ClosingBrace { get; }

        protected AbstractTokenBraceCompletionSession(
            ISyntaxFactsService syntaxFactsService,
            int openingTokenKind,
            int closingTokenKind,
            char openingBrace,
            char closingBrace)
        {
            _syntaxFactsService = syntaxFactsService;
            OpeningTokenKind = openingTokenKind;
            ClosingTokenKind = closingTokenKind;
            OpeningBrace = openingBrace;
            ClosingBrace = closingBrace;
        }

        public virtual bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var document = session.Document;
            var position = session.OpeningPoint;
            var token = document.FindToken(position, cancellationToken);

            if (!IsValidToken(token))
            {
                return false;
            }

            return token.RawKind == OpeningTokenKind && token.SpanStart == position;
        }

        protected bool IsValidToken(SyntaxToken token)
        {
            return token.Parent != null && !_syntaxFactsService.IsSkippedTokensTrivia(token.Parent);
        }

        public virtual void AfterStart(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
        }

        public virtual void AfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
        }

        public virtual bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            return CheckCurrentPosition(session, cancellationToken) && CheckClosingTokenKind(session, cancellationToken);
        }

        protected bool CheckClosingTokenKind(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var document = session.Document;
            if (document != null)
            {
                var root = document.GetSyntaxRootSynchronously(cancellationToken);
                var position = session.ClosingPoint;

                return root.FindTokenFromEnd(position, includeZeroWidth: false, findInsideTrivia: true).RawKind == this.ClosingTokenKind;
            }

            return true;
        }

        protected bool CheckCurrentPosition(IBraceCompletionSession session, CancellationToken cancellationToken)
        {
            var document = session.Document;
            if (document != null)
            {
                // make sure auto closing is called from a valid position
                var tree = document.GetSyntaxRootSynchronously(cancellationToken).SyntaxTree;

                return !_syntaxFactsService.IsInNonUserCode(tree, session.CaretPosition, cancellationToken);
            }

            return true;
        }
    }
}
