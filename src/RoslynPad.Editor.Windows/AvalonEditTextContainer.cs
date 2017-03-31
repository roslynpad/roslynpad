using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Text;
using TextChangeEventArgs = Microsoft.CodeAnalysis.Text.TextChangeEventArgs;

namespace RoslynPad.Editor.Windows
{
    internal sealed class AvalonEditTextContainer : SourceTextContainer, IUpdatableTextContainer, IDisposable
    {
        private readonly TextEditor _editor;

        private SourceText _currentText;
        private bool _updatding;

        public TextDocument Document => _editor.Document;

        public override SourceText CurrentText => _currentText;

        public AvalonEditTextContainer(TextEditor editor)
        {
            _editor = editor;
            _currentText = new AvalonEditSourceText(this, _editor.Text);

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
            var caret = _editor.CaretOffset;
            var offset = 0;
            try
            {
                var changes = newText.GetTextChanges(_currentText);
                
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
                var carretOffset = caret + offset;
                if (carretOffset < 0)
                    carretOffset = 0;
                if (carretOffset > newText.Length)
                    carretOffset = newText.Length;
                _editor.CaretOffset = carretOffset;
                _editor.Document.EndUpdate();
            }
        }

        private class AvalonEditSourceText : SourceText
        {
            private readonly AvalonEditTextContainer _container;
            private readonly SourceText _sourceText;

            public AvalonEditSourceText(AvalonEditTextContainer container, string text) : this(container, From(text))
            {
            }

            private AvalonEditSourceText(AvalonEditTextContainer container, SourceText sourceText)
            {
                _container = container;
                _sourceText = sourceText;
            }

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                _sourceText.CopyTo(sourceIndex, destination, destinationIndex, count);
            }

            public override Encoding Encoding => _sourceText.Encoding;

            public override int Length => _sourceText.Length;

            public override char this[int position] => _sourceText[position];

            public override SourceText GetSubText(TextSpan span) => new AvalonEditSourceText(_container, _sourceText.GetSubText(span));

            public override void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = new CancellationToken())
            {
                _sourceText.Write(writer, span, cancellationToken);
            }

            public override string ToString() => _sourceText.ToString();

            public override string ToString(TextSpan span) => _sourceText.ToString(span);
            
            public override IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
                => _sourceText.GetChangeRanges(oldText);

            public override IReadOnlyList<TextChange> GetTextChanges(SourceText oldText) => _sourceText.GetTextChanges(oldText);

            protected override TextLineCollection GetLinesCore() => _sourceText.Lines;

            protected override bool ContentEqualsImpl(SourceText other) => _sourceText.ContentEquals(other);

            public override SourceTextContainer Container => _container ?? _sourceText.Container;

            public override bool Equals(object obj) => _sourceText.Equals(obj);

            public override int GetHashCode() => _sourceText.GetHashCode();

            public override SourceText WithChanges(IEnumerable<TextChange> changes)
            {
                return new AvalonEditSourceText(_container, _sourceText.WithChanges(changes));
            }
        }
    }
}
