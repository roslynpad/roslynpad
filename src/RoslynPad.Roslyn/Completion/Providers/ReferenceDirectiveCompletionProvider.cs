using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynPad.Roslyn.Completion.Providers
{
    [ExportCompletionProvider("ReferenceDirectiveCompletionProvider", LanguageNames.CSharp)]
    internal class ReferenceDirectiveCompletionProvider : AbstractReferenceDirectiveCompletionProvider
    {
        protected override bool TryGetStringLiteralToken(SyntaxTree tree, int position, out SyntaxToken stringLiteral, CancellationToken cancellationToken) =>
            tree.TryGetStringLiteralToken(position, SyntaxKind.ReferenceDirectiveTrivia, out stringLiteral, cancellationToken);

    }
}