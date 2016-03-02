using System;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.Text;
using TextChangeEventArgs = Microsoft.CodeAnalysis.Text.TextChangeEventArgs;

namespace RoslynPad.RoslynEditor
{
    internal sealed class AvalonEditTextContainer : SourceTextContainer, IDisposable
    {
        private readonly TextEditor _editor;

        private SourceText _currentText;
        private bool _updatding;

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
            if (_updatding) return;

            var oldText = _currentText;

            var textSpan = new TextSpan(e.Offset, e.RemovalLength);
            var textChangeRange = new TextChangeRange(textSpan, e.InsertionLength);
            _currentText = _currentText.WithChanges(new TextChange(textSpan, e.InsertedText?.Text ?? string.Empty));

            TextChanged?.Invoke(this, new TextChangeEventArgs(oldText, _currentText, textChangeRange));
        }

        public override event EventHandler<TextChangeEventArgs> TextChanged;

        public void UpdateText(SourceText newText)
        {
            _updatding = true;
            _editor.Document.BeginUpdate();
            try
            {
                var changes = newText.GetTextChanges(_currentText);

                var offset = 0;

                foreach (var change in changes)
                {
                    _editor.Document.Replace(change.Span.Start + offset, change.Span.Length, new StringTextSource(change.NewText));

                    offset += change.NewText.Length - change.Span.Length;
                }

                _currentText = newText;
            }
            finally
            {
                _updatding = false;
                _editor.Document.EndUpdate();
            }
        }
    }
}
