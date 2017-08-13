using Avalonia.Media;
using AvaloniaEdit.Text;

namespace RoslynPad.Editor
{
    public static class AvaloniaEditExtensions
    {
        public static void SetForegroundBrush(this TextRunProperties properties, IBrush brush)
        {
            properties.ForegroundBrush = brush;
        }
    }
}