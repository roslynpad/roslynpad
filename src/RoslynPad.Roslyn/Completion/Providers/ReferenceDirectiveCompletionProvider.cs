using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace RoslynPad.Roslyn.Completion.Providers
{
    [ExportCompletionProvider("ReferenceDirectiveCompletionProvider", LanguageNames.CSharp)]
    internal class ReferenceDirectiveCompletionProvider : AbstractReferenceDirectiveCompletionProvider
    {
        protected override bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken) =>
            TryGetStringLiteralToken(tree, position, SyntaxKind.ReferenceDirectiveTrivia, out stringLiteral, cancellationToken);

        internal static bool TryGetStringLiteralToken(SyntaxTree tree, int position, SyntaxKind directiveKind, out SyntaxToken stringLiteral, CancellationToken cancellationToken)
        {
            if (tree.IsEntirelyWithinStringLiteral(position, cancellationToken))
            {
                var token = tree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
                if (token.Kind() == SyntaxKind.EndOfDirectiveToken || token.Kind() == SyntaxKind.EndOfFileToken)
                {
                    token = token.GetPreviousToken(includeSkipped: true, includeDirectives: true);
                }

                if (token.Kind() == SyntaxKind.StringLiteralToken && token.Parent.Kind() == directiveKind)
                {
                    stringLiteral = token;
                    return true;
                }
            }

            stringLiteral = default(SyntaxToken);
            return false;
        }

    }
}