using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal interface IBraceCompletionSession
    {
        Document Document { get; set; }
        SourceText Text { get; }

        int OpeningPoint { get; }
        int ClosingPoint { get; }
        char OpeningBrace { get; }
        char ClosingBrace { get; }

        int CaretPosition { get; }
        void TryMoveCaret(int position);
    }
}