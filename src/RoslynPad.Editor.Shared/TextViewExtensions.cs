#if AVALONIA
using Avalonia;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
#else
using System.Windows;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
#endif

namespace RoslynPad.Editor
{
    internal static class TextViewExtensions
    {
        public static Point GetPosition(this TextView textView, int line, int column)
        {
            var visualPosition = textView.GetVisualPosition(
                new TextViewPosition(line, column), VisualYPosition.LineBottom) - textView.ScrollOffset;
            return visualPosition;
        }
    }
}