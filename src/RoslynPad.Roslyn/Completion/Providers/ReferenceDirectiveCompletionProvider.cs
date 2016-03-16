using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor;

namespace RoslynPad.Roslyn.Completion.Providers
{
    [ExportCompletionProvider("ReferenceDirectiveCompletionProvider", LanguageNames.CSharp)]
    internal class ReferenceDirectiveCompletionProvider : AbstractReferenceDirectiveCompletionProvider
    {
        protected override bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken)
        {
            if (IsEntirelyWithinStringLiteral(tree, position, cancellationToken))
            {
                var token = tree.GetRoot(cancellationToken).FindToken(position, true);
                if (token.Kind() == SyntaxKind.EndOfDirectiveToken || token.Kind() == SyntaxKind.EndOfFileToken)
                {
                    token = token.GetPreviousToken(includeSkipped: true, includeDirectives: true);
                }

                if (token.Kind() == SyntaxKind.StringLiteralToken &&
                    token.Parent.Kind() == SyntaxKind.ReferenceDirectiveTrivia)
                {
                    stringLiteral = token;
                    return true;
                }
            }

            stringLiteral = default(SyntaxToken);
            return false;
        }

        #region SyntaxTreeExtensions

        // TODO: access Microsoft.CodeAnalysis.Workspaces internals and remove this

        public static bool IsKind(SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2)
        {
            return token.Kind() == kind1
                   || token.Kind() == kind2;
        }

        public static bool IsKind(SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            return token.Kind() == kind1
                   || token.Kind() == kind2
                   || token.Kind() == kind3;
        }

        public static bool IsEntirelyWithinStringLiteral(
            SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);

            // If we ask right at the end of the file, we'll get back nothing. We handle that case
            // specially for now, though SyntaxTree.FindToken should work at the end of a file.
            if (IsKind(token, SyntaxKind.EndOfDirectiveToken, SyntaxKind.EndOfFileToken))
            {
                token = token.GetPreviousToken(includeSkipped: true, includeDirectives: true);
            }

            if (token.IsKind(SyntaxKind.StringLiteralToken))
            {
                var span = token.Span;

                // cases:
                // "|"
                // "|  (e.g. incomplete string literal)
                return (position > span.Start && position < span.End)
                       || AtEndOfIncompleteStringOrCharLiteral(token, position, '"');
            }

            if (IsKind(token, SyntaxKind.InterpolatedStringStartToken, SyntaxKind.InterpolatedStringTextToken,
                SyntaxKind.InterpolatedStringEndToken))
            {
                return token.SpanStart < position && token.Span.End > position;
            }

            return false;
        }

        private static bool AtEndOfIncompleteStringOrCharLiteral(SyntaxToken token, int position, char lastChar)
        {
            if (!IsKind(token, SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken))
            {
                throw new ArgumentException("ExpectedStringOrCharLiteral", nameof(token));
            }

            int startLength = 1;
            if (token.IsVerbatimStringLiteral())
            {
                startLength = 2;
            }

            return position == token.Span.End &&
                   (token.Span.Length == startLength ||
                    (token.Span.Length > startLength && token.ToString().LastOrDefault() != lastChar));
        }

        #endregion
    }
}