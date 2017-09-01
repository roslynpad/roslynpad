using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal interface IEditorBraceCompletionSession : ILanguageService
    {
        char OpeningBrace { get; }
        char ClosingBrace { get; }

        bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken);
        void AfterStart(IBraceCompletionSession session, CancellationToken cancellationToken);
        bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken);
        void AfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken);
    }
}