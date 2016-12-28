using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;

namespace RoslynPad.Editor.Windows
{
    internal static class DocumentUtilities
    {
        public static int FindPreviousWordStart(this ITextSource textSource, int offset)
        {
            return TextUtilities.GetNextCaretPosition(textSource, offset, LogicalDirection.Backward, CaretPositioningMode.WordStart);
        }
    }
}