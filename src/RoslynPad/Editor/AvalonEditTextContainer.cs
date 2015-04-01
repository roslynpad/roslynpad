using System;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.Text;
using TextChangeEventArgs = Microsoft.CodeAnalysis.Text.TextChangeEventArgs;

namespace RoslynPad.Editor
{
    class AvalonEditTextContainer : SourceTextContainer
    {
        private readonly TextEditor _editor;

        private SourceText _before;
        private SourceText _current;

        public TextDocument Document
        {
            get { return _editor.Document; }
        }

        public AvalonEditTextContainer(TextEditor editor)
        {
            _editor = editor;
            SetCurrent();

            _editor.Document.Changing += DocumentOnChanging;
            _editor.Document.Changed += DocumentOnChanged;
        }

        private void SetCurrent()
        {
            _current = SourceText.From(_editor.Text);
        }

        private void DocumentOnChanging(object sender, DocumentChangeEventArgs e)
        {
            _before = CurrentText;
        }

        private void DocumentOnChanged(object sender, DocumentChangeEventArgs e)
        {
            SetCurrent();

            var textChangeRange = new TextChangeRange(
                new TextSpan(e.Offset, e.RemovalLength),
                e.RemovalLength == 0 ? e.InsertionLength : e.RemovalLength);
            OnTextChanged(new TextChangeEventArgs(_before, CurrentText, textChangeRange));
        }

        public override event EventHandler<TextChangeEventArgs> TextChanged;

        public override SourceText CurrentText
        {
            get { return _current; }
        }

        protected virtual void OnTextChanged(TextChangeEventArgs e)
        {
            var handler = TextChanged;
            if (handler != null) handler(this, e);
        }
    }
}
