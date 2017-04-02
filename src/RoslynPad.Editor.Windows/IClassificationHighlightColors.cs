using ICSharpCode.AvalonEdit.Highlighting;

namespace RoslynPad.Editor.Windows
{
    public interface IClassificationHighlightColors
    {
        HighlightingColor DefaultBrush { get; }

        HighlightingColor GetBrush(string classificationTypeName);
    }
}