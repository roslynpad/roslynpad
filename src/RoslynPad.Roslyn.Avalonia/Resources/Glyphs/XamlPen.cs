using System;
using Avalonia.Media;
using Portable.Xaml.Markup;

namespace RoslynPad.Roslyn.Resources
{
    public class PenExtension : MarkupExtension
    {
        public IBrush? Brush { get; set; }

        public double Thickness { get; set; } = 1.0;

        public DashStyle? DashStyle { get; set; }

        public PenLineCap DashCap { get; set; }

        public PenLineCap StartLineCap { get; set; }

        public PenLineCap EndLineCap { get; set; }

        public PenLineJoin LineJoin { get; set; } = PenLineJoin.Miter;

        public double MiterLimit { get; set; } = 10.0;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Pen(Brush, Thickness, DashStyle, DashCap, StartLineCap, EndLineCap, LineJoin, MiterLimit);
        }
    }
}