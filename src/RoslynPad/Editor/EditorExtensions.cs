using ICSharpCode.AvalonEdit;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Editor
{
    public static class EditorExtensions
    {
        public static SourceText AsText(this TextEditor editor)
        {
            return SourceText.From(editor.Text);
        }

        public static SourceTextContainer AsTextContainer(this TextEditor editor)
        {
            return new AvalonEditTextContainer(editor);
        }
    }
}
