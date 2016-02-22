using System;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.Text;
using TextChangeEventArgs = Microsoft.CodeAnalysis.Text.TextChangeEventArgs;

namespace RoslynPad.Editor
{
    internal sealed class AvalonEditTextContainer : SourceTextContainer, IDisposable
    {
        private readonly TextEditor _editor;

        private SourceText _currentText;

        public TextDocument Document => _editor.Document;

        public override SourceText CurrentText => _currentText;

        public AvalonEditTextContainer(TextEditor editor)
        {
            _editor = editor;
            _currentText = SourceText.From(_editor.Text);

            _editor.Document.Changed += DocumentOnChanged;
        }

        public void Dispose()
        {
            _editor.Document.Changed -= DocumentOnChanged;
        }

        private void DocumentOnChanged(object sender, DocumentChangeEventArgs e)
        {
            var oldText = _currentText;

            var textSpan = new TextSpan(e.Offset, e.RemovalLength);
            var textChangeRange = new TextChangeRange(textSpan, e.InsertionLength);
            _currentText = _currentText.WithChanges(new TextChange(textSpan, e.InsertedText?.Text ?? string.Empty));

            TextChanged?.Invoke(this, new TextChangeEventArgs(oldText, _currentText, textChangeRange));
        }

        public override event EventHandler<TextChangeEventArgs> TextChanged;
    }
}
