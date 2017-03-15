using System;
using System.Collections;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace RoslynPad.Editor.Windows
{
    public class ErrorMargin : AbstractMargin
    {
        private readonly Ellipse _marker;

        public ErrorMargin()
        {
            _marker = CreateMarker();
        }

        private Ellipse CreateMarker()
        {
            var marker = new Ellipse();
            marker.SetBinding(Shape.FillProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MarkerBrush)) });
            marker.SetBinding(ToolTipProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Message)) });
            AddLogicalChild(marker);
            AddVisualChild(marker);
            return marker;
        }

        protected override IEnumerator LogicalChildren
        {
            get { yield return _marker; }
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return _marker;
        }

        public static readonly DependencyProperty LineNumberProperty = DependencyProperty.Register(
            nameof(LineNumber), typeof(int?), typeof(ErrorMargin), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange));

        public int? LineNumber
        {
            get => (int?)GetValue(LineNumberProperty);
            set => SetValue(LineNumberProperty, value);
        }

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            nameof(Message), typeof(string), typeof(ErrorMargin), new FrameworkPropertyMetadata());

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty MarkerBrushProperty = DependencyProperty.Register(
            nameof(MarkerBrush), typeof(Brush), typeof(ErrorMargin), new FrameworkPropertyMetadata());

        public Brush MarkerBrush
        {
            get => (Brush)GetValue(MarkerBrushProperty);
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
            var visibility = Visibility.Collapsed;
            if (lineNumber != null && textView != null)
            {
                var line = textView.GetVisualLine(lineNumber.Value);
                if (line != null)
                {
                    visibility = Visibility.Visible;
                    var visualYPosition = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                    _marker.Arrange(new Rect(
                        new Point(0, visualYPosition - textView.VerticalOffset),
                        new Size(finalSize.Width, finalSize.Width)));
                }
            }
            _marker.Visibility = visibility;
            return base.ArrangeOverride(finalSize);
        }
    }
}