using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal abstract class AbstractEditorBraceCompletionSessionFactory : IEditorBraceCompletionSessionFactory
    {
        protected AbstractEditorBraceCompletionSessionFactory()
        {
        }

        protected abstract bool IsSupportedOpeningBrace(char openingBrace);

        protected abstract IEditorBraceCompletionSession CreateEditorSession(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken);

        public IEditorBraceCompletionSession TryCreateSession(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
        {
            if (IsSupportedOpeningBrace(openingBrace) &&
                CheckCodeContext(document, openingPosition, openingBrace, cancellationToken))
            {
                return CreateEditorSession(document, openingPosition, openingBrace, cancellationToken);
            }

            return null;
        }

        protected virtual bool CheckCodeContext(Document document, int position, char openingBrace, CancellationToken cancellationToken)
        {
            // check that the user is not typing in a string literal or comment
            var tree = document.GetSyntaxRootSynchronously(cancellationToken).SyntaxTree;
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            return !syntaxFactsService.IsInNonUserCode(tree, position, cancellationToken);
        }
    }
}