using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.AutomaticCompletion
{
    public interface IBraceCompletionProvider
    {
        bool TryComplete(Document document, int position);
    }

    [Export(typeof(IBraceCompletionProvider)), Shared]
    internal class BraceCompletionProvider : IBraceCompletionProvider
    {
        public bool TryComplete(Document document, int position)
        {
            if (!document.TryGetText(out var text))
            {
                return false;
            }

            var editorSessionFactory = document.GetLanguageService<IEditorBraceCompletionSessionFactory>();
            var editorSession = editorSessionFactory.TryCreateSession(document, position - 1, text[position - 1], CancellationToken.None);
            if (editorSession == null)
            {
                return false;
            }

            var caretProvider = text.Container as IEditorCaretProvider;
            var session = new Session(caretProvider, document, text)
            {
                OpeningPoint = position - 1,
                ClosingPoint = position,
                OpeningBrace = editorSession.OpeningBrace,
                ClosingBrace = editorSession.ClosingBrace,
            };

            if (!editorSession.CheckOpeningPoint(session, CancellationToken.None))
            {
                return false;
            }

            // insert the closing brace
            session.Document.InsertText(position, session.ClosingBrace.ToString());

            // move the caret back between the braces
            caretProvider?.TryMoveCaret(position);

            editorSession.AfterStart(session, CancellationToken.None);
            return true;
        }

        private class Session : IBraceCompletionSession
        {
            private readonly IEditorCaretProvider? _caretProvider;

            public Session(IEditorCaretProvider? caretProvider, Document document, SourceText text)
            {
                _caretProvider = caretProvider;
                Document = document;
                Text = text;
            }

            public Document Document { get; set; }

            public SourceText Text { get; set; }

            public int OpeningPoint { get; set; }

            public int ClosingPoint { get; set; }

            public char OpeningBrace { get; set; }

            public char ClosingBrace { get; set; }

            public int CaretPosition => _caretProvider?.CaretPosition ?? 0;

            public void TryMoveCaret(int position) => _caretProvider?.TryMoveCaret(position);
        }
    }
}