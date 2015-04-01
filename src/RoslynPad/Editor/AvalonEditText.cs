using System.Text;
using ICSharpCode.AvalonEdit;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Editor
{
    internal sealed class AvalonEditText : SourceText
    {
        private readonly TextEditor _editor;

        public AvalonEditText(TextEditor editor)
        {
            _editor = editor;
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                destination[i + destinationIndex] = _editor.Document.GetCharAt(sourceIndex);
            }
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }

        public override int Length
        {
            get { return _editor.Document.TextLength; }
        }

        public override char this[int position]
        {
            get { return _editor.Document.GetCharAt(position); }
        }
    }
}
