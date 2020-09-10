using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
#if AVALONIA
using AvaloniaEdit;
using AvaloniaEdit.Document;
#else
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
#endif
using Microsoft.CodeAnalysis.Text;
using TextChangeEventArgs = Microsoft.CodeAnalysis.Text.TextChangeEventArgs;
using RoslynPad.Roslyn;

namespace RoslynPad.Editor
{
    public sealed class AvalonEditTextContainer : SourceTextContainer, IEditorCaretProvider, IDisposable
    {
        private SourceText _currentText;
        private bool _updatding;

        public TextDocument Document { get; }

        /// <summary>
        /// If set, <see cref="TextEditor.CaretOffset"/> will be updated.
        /// </summary>
        public TextEditor? Editor { get; set; }

        public override SourceText CurrentText => _currentText;
        
        public AvalonEditTextContainer(TextDocument document)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            _currentText = new AvalonEditSourceText(this, Document.Text);

            Document.Changed += DocumentOnChanged;
        }

        public void Dispose()
        {
            Document.Changed -= DocumentOnChanged;
        }

        private void DocumentOnChanged(object? sender, DocumentChangeEventArgs e)
        {
            if (_updatding) return;

            var oldText = _currentText;

            var textSpan = new TextSpan(e.Offset, e.RemovalLength);
            var textChangeRange = new TextChangeRange(textSpan, e.InsertionLength);
            _currentText = _currentText.WithChanges(new TextChange(textSpan, e.InsertedText?.Text ?? string.Empty));

            TextChanged?.Invoke(this, new TextChangeEventArgs(oldText, _currentText, textChangeRange));
        }

        public override event EventHandler<TextChangeEventArgs>? TextChanged;

        public void UpdateText(SourceText newText)
        {
            _updatding = true;
            Document.BeginUpdate();
            var editor = Editor;
            var caretOffset = editor?.CaretOffset ?? 0;
            var documentOffset = 0;
            try
            {
                var changes = newText.GetTextChanges(_currentText);
                
                foreach (var change in changes)
                {
                    var newTextChange = change.NewText ?? string.Empty;
                    Document.Replace(change.Span.Start + documentOffset, change.Span.Length, new StringTextSource(newTextChange));
                    
                    var changeOffset = newTextChange.Length - change.Span.Length;
                    if (caretOffset >= change.Span.Start + documentOffset + change.Span.Length)
                    {
                        // If caret is after text, adjust it by text size difference
                        caretOffset += changeOffset;
                    }
                    else if (caretOffset >= change.Span.Start + documentOffset)
                    {
                        // If caret is inside changed text, but go out of bounds of the replacing text after the change, go back inside
                        if (caretOffset >= change.Span.Start + documentOffset + newTextChange.Length)
                        {
                            caretOffset = change.Span.Start + documentOffset;
                        }
                    }

                    documentOffset += changeOffset;
                }

                _currentText = newText;
            }
            finally
            {
                _updatding = false;
                Document.EndUpdate();
                if (caretOffset < 0)
                    caretOffset = 0;
                if (caretOffset > newText.Length)
                    caretOffset = newText.Length;
                if (editor != null)
                    editor.CaretOffset = caretOffset;
            }
        }

        int IEditorCaretProvider.CaretPosition => Editor?.CaretOffset ?? 0;

        bool IEditorCaretProvider.TryMoveCaret(int position)
        {
            if (Editor != null)
            {
                Editor.CaretOffset = position;
            }
            return true;
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

            public override Encoding? Encoding => _sourceText.Encoding;

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
                => _sourceText.GetChangeRanges(GetInnerSourceText(oldText));

            public override IReadOnlyList<TextChange> GetTextChanges(SourceText oldText) => _sourceText.GetTextChanges(GetInnerSourceText(oldText));

            protected override TextLineCollection GetLinesCore() => _sourceText.Lines;

            protected override bool ContentEqualsImpl(SourceText other) => _sourceText.ContentEquals(GetInnerSourceText(other));

            public override SourceTextContainer Container => _container ?? _sourceText.Container;

            public override bool Equals(object? obj) => _sourceText.Equals(obj);

            public override int GetHashCode() => _sourceText.GetHashCode();

            public override SourceText WithChanges(IEnumerable<TextChange> changes)
            {
                return new AvalonEditSourceText(_container, _sourceText.WithChanges(changes));
            }

            private static SourceText GetInnerSourceText(SourceText oldText)
            {
                return (oldText as AvalonEditSourceText)?._sourceText ?? oldText;
            }
        }
    }
}
