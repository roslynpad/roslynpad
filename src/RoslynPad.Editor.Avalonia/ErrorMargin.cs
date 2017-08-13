using System;
using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using Avalonia.Controls;

namespace RoslynPad.Editor
{
    public class ErrorMargin : AbstractMargin
    {
        private readonly Ellipse _marker;

        static ErrorMargin()
        {
            AffectsRender(LineNumberProperty);
        }

        public ErrorMargin()
        {
            _marker = CreateMarker();
        }

        private Ellipse CreateMarker()
        {
            var marker = new Ellipse();
            marker[~Shape.FillProperty] = this[~MarkerBrushProperty];
            marker[~ToolTip.TipProperty] = this[~MessageProperty];
            VisualChildren.Add(marker);
            LogicalChildren.Add(marker);
            return marker;
        }

        public static readonly StyledProperty<int?> LineNumberProperty =
            AvaloniaProperty.Register<ErrorMargin, int?>(nameof(LineNumber));

        public int? LineNumber
        {
            get => GetValue(LineNumberProperty);
            set => SetValue(LineNumberProperty, value);
        }

        public static readonly StyledProperty<string> MessageProperty = 
            AvaloniaProperty.Register<ErrorMargin, string>(nameof(Message));

        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly StyledProperty<IBrush> MarkerBrushProperty = 
            AvaloniaProperty.Register<ErrorMargin, IBrush>(nameof(MarkerBrush));

        public IBrush MarkerBrush
        {
            get => GetValue(MarkerBrushProperty);
            set => SetValue(MarkerBrushProperty, value);
        }

        protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
        {
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
            }
            base.OnTextViewChanged(oldTextView, newTextView);
            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
            }
            
            InvalidateArrange();
        }

        private void TextViewVisualLinesChanged(object sender, EventArgs e)
        {
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            _marker.Measure(availableSize);
            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var lineNumber = LineNumber;
            var textView = TextView;
            var isVisible = false;
            if (lineNumber != null && textView != null)
            {
                var line = textView.GetVisualLine(lineNumber.Value);
                if (line != null)
                {
                    isVisible = true;
                    var visualYPosition = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                    _marker.Arrange(new Rect(
                        new Point(0, visualYPosition - textView.VerticalOffset),
                        new Size(finalSize.Width, finalSize.Width)));
                }
            }
            _marker.IsVisible = isVisible;
            return base.ArrangeOverride(finalSize);
        }
    }
}