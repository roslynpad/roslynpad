#if AVALONIA
using AvaloniaEdit.Document;
#else
using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;
#endif

namespace RoslynPad.Editor
{
    internal static class DocumentUtilities
    {
        public static int FindPreviousWordStart(this ITextSource textSource, int offset)
        {
            return TextUtilities.GetNextCaretPosition(textSource, offset, LogicalDirection.Backward, CaretPositioningMode.WordStart);
        }
    }
}