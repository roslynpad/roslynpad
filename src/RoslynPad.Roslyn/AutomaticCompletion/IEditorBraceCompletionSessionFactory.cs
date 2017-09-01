using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using System.Threading;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal interface IEditorBraceCompletionSessionFactory : ILanguageService
    {
        IEditorBraceCompletionSession TryCreateSession(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken);
    }
}