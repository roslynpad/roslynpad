using System;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using Avalonia.Controls;

namespace RoslynPad.Editor
{
    public class MarkerMargin : AbstractMargin
    {
        static MarkerMargin()
        {
            LineNumberProperty.Changed.AddClassHandler<MarkerMargin>((o, e) => o.InvalidateArrange());
        }

        public MarkerMargin()
        {
            Marker = CreateMarker();
        }

        public event EventHandler? MarkerPointerDown;

        private Control CreateMarker()
        {
            var marker = new DrawingPresenter();
            marker.PointerPressed += (o, e) => { e.Handled = true; MarkerPointerDown?.Invoke(o, e); };
            marker[~DrawingPresenter.DrawingProperty] = this[~MarkerImageProperty];
            marker[~ToolTip.TipProperty] = this[~MessageProperty];
            VisualChildren.Add(marker);
            LogicalChildren.Add(marker);
            return marker;
        }

        public static readonly StyledProperty<int?> LineNumberProperty =
            AvaloniaProperty.Register<MarkerMargin, int?>(nameof(LineNumber));

        public int? LineNumber
        {
            get => GetValue(LineNumberProperty);
            set => SetValue(LineNumberProperty, value);
        }

        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<MarkerMargin, string>(nameof(Message));

        public string Message
        {
            get => GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly StyledProperty<Drawing?> MarkerImageProperty =
            AvaloniaProperty.Register<MarkerMargin, Drawing?>(nameof(MarkerImage));

        public Drawing? MarkerImage
        {
            get => GetValue(MarkerImageProperty);
            set => SetValue(MarkerImageProperty, value);
        }

        public Control Marker { get; }

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
            Marker.Measure(availableSize);
            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var lineNumber = LineNumber;
            var textView = TextView;
            if (lineNumber != null && textView?.GetVisualLine(lineNumber.Value) is VisualLine line)
            {
                Marker.IsVisible = true;
                var visualYPosition = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
                Marker.Arrange(new Rect(
                    new Point(0, visualYPosition - textView.VerticalOffset),
                    new Size(finalSize.Width, finalSize.Width)));
            }
            else
            {
                Marker.IsVisible = false;
            }

            return finalSize;
        }
    }
}
