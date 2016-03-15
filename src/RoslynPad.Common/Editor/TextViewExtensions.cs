using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;

namespace RoslynPad.Editor
{
    internal static class TextViewExtensions
    {
        public static Point GetScreenPosition(this TextView textView, int line, int column)
        {
            var visualPosition = GetPosition(textView, line, column);
            var positionInPixels = textView.PointToScreen(new Point(visualPosition.X.CoerceValue(0, textView.ActualWidth),
                visualPosition.Y.CoerceValue(0, textView.ActualHeight)));
            return positionInPixels.TransformFromDevice(textView);
        }

        public static Point GetPosition(this TextView textView, int line, int column)
        {
            var visualPosition = textView.GetVisualPosition(
                new TextViewPosition(line, column), VisualYPosition.LineBottom) - textView.ScrollOffset;
            return visualPosition;
        }

        public static double CoerceValue(this double value, double minimum, double maximum)
        {
            return Math.Max(Math.Min(value, maximum), minimum);
        }

        public static int CoerceValue(this int value, int minimum, int maximum)
        {
            return Math.Max(Math.Min(value, maximum), minimum);
        }

        private static Point TransformFromDevice(this Point point, Visual visual)
        {
            var compositionTarget = PresentationSource.FromVisual(visual)?.CompositionTarget;
            if (compositionTarget == null) throw new InvalidOperationException("Invalid visual");
            var matrix = compositionTarget.TransformFromDevice;
            return new Point(point.X * matrix.M11, point.Y * matrix.M22);
        }
    }
}