using ICSharpCode.AvalonEdit;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Editor
{
    internal static class EditorExtensions
    {
        public static SourceTextContainer AsTextContainer(this TextEditor editor)
        {
            return new AvalonEditTextContainer(editor);
        }
    }
}
