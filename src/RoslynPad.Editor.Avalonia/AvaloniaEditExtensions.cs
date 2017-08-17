using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Text;

namespace RoslynPad.Editor
{
    public static class AvaloniaEditExtensions
    {
        public static void SetForegroundBrush(this TextRunProperties properties, IBrush brush)
        {
            properties.ForegroundBrush = brush;
        }

        public static bool IsOpen(this CompletionWindowBase window) => window?.IsEffectivelyVisible == true;
    }
}