using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
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
        private bool _updatding;

        public TextDocument Document => _editor.Document;

        public override SourceText CurrentText => _currentText;

        public AvalonEditTextContainer(TextEditor editor)
        {
            _editor = editor;
            _currentText = new AvalonEditStringText(_editor.Text, null, container: this);

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
            _editor.Document.UndoStack.StartUndoGroup();
            try
            {
                var changes = newText.GetTextChanges(_currentText);

                foreach (var change in changes)
                {
                    _editor.Document.Replace(change.Span.Start, change.Span.Length, new StringTextSource(change.NewText));
                }

                _currentText = newText;
            }
            finally
            {
                _updatding = false;
                _editor.Document.UndoStack.EndUndoGroup();
            }
        }
    }

    internal sealed class AvalonEditStringText : SourceText
    {
        private readonly string _source;

        internal AvalonEditStringText(string source, Encoding encodingOpt, AvalonEditTextContainer container, ImmutableArray<byte> checksum = default(ImmutableArray<byte>), SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
            : base(checksum, checksumAlgorithm, container)
        {
            Debug.Assert(source != null);

            _source = source;
            Encoding = encodingOpt;
        }

        public override Encoding Encoding { get; }

        public string Source => _source;

        public override int Length => _source.Length;

        public override char this[int position] => _source[position];

        public override string ToString(TextSpan span)
        {
            if (span.End > Source.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            if (span.Start == 0 && span.Length == Length)
            {
                return Source;
            }

            return Source.Substring(span.Start, span.Length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            Source.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override void Write(TextWriter textWriter, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (span.Start == 0 && span.End == Length)
            {
                textWriter.Write(Source);
            }
            else
            {
                base.Write(textWriter, span, cancellationToken);
            }
        }
    }
}
