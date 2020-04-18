using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace RoslynPad.Editor
{
    public class MarkerMargin : AbstractMargin
    {
        public MarkerMargin()
        {
            Marker = CreateMarker();
        }

        private FrameworkElement CreateMarker()
        {
            var marker = new Image();
            marker.MouseDown += (o, e) => { e.Handled = true; MarkerPointerDown?.Invoke(o, e); };
            marker.SetBinding(Image.SourceProperty, new Binding { Source = this, Path = new PropertyPath(nameof(MarkerImage)) });
            marker.SetBinding(ToolTipProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Message)) });
            AddLogicalChild(marker);
            AddVisualChild(marker);
            return marker;
        }

        public event EventHandler? MarkerPointerDown;

        public FrameworkElement Marker { get; private set; }

        protected override IEnumerator LogicalChildren
        {
            get { yield return Marker; }
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Marker;
        }

        public static readonly DependencyProperty LineNumberProperty = DependencyProperty.Register(
            nameof(LineNumber), typeof(int?), typeof(MarkerMargin), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange));

        public int? LineNumber
        {
            get => (int?)GetValue(LineNumberProperty);
            set => SetValue(LineNumberProperty, value);
        }

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            nameof(Message), typeof(string), typeof(MarkerMargin), new FrameworkPropertyMetadata());

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty MarkerImageProperty = DependencyProperty.Register(
            nameof(MarkerImage), typeof(ImageSource), typeof(MarkerMargin), new FrameworkPropertyMetadata());

        public ImageSource? MarkerImage
        {
            get => (ImageSource?)GetValue(MarkerImageProperty);
            set => SetValue(MarkerImageProperty, value);
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

        private void TextViewVisualLinesChanged(object? sender, EventArgs e)
        {
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Marker.Measure(availableSize);
            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var lineNumber = LineNumber;
            var textView = TextView;

            if (lineNumber != null && textView?.GetVisualLine(lineNumber.Value) is VisualLine line)
            {
                    Marker.Visibility = Visibility.Visible;
                    var visualYPosition = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                    Marker.Arrange(new Rect(
                        new Point(0, visualYPosition - textView.VerticalOffset),
                        new Size(finalSize.Width, finalSize.Width)));
            }
            else
            {
                Marker.Visibility = Visibility.Collapsed;
            }

            return base.ArrangeOverride(finalSize);
        }
    }
}