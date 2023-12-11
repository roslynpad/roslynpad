using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace RoslynPad
{
    public class DiffInfoMargin : AbstractMargin
    {
        static readonly Brush AddedBackground;
        static readonly Brush DeletedBackground;
        static readonly Brush BlankBackground;

        static readonly SolidColorBrush BackBrush;
        static readonly SolidColorBrush ForegroundBrush;

        static readonly Pen BorderlessPen;

        const double TextHorizontalMargin = 4.0;


#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        static DiffInfoMargin()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            AddedBackground = new SolidColorBrush(Color.FromRgb(0xdd, 0xff, 0xdd));
            AddedBackground.Freeze();

            DeletedBackground = new SolidColorBrush(Color.FromRgb(0xff, 0xdd, 0xdd));
            DeletedBackground.Freeze();

            BlankBackground = new SolidColorBrush(Color.FromRgb(0xfa, 0xfa, 0xfa));
            BlankBackground.Freeze();

            var transparentBrush = new SolidColorBrush(Colors.Transparent);
            transparentBrush.Freeze();

            BorderlessPen = new Pen(transparentBrush, 0.0);
            BorderlessPen.Freeze();

            BackBrush = new SolidColorBrush(Color.FromRgb(255, 0, 255));
            BackBrush.Freeze();

            ForegroundBrush = new SolidColorBrush(Colors.DarkGray);
            ForegroundBrush.Freeze();
        }
        public DiffInfoMargin(CompareDocuemnt doc)
        {
            Lines = doc;
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            FormattedText _lineFt, _plusMinusFt;
            if (Lines == null || Lines.Count == 0) return new Size(0.0, 0.0);

            var textToUse = Lines.Last().Number.ToString(CultureInfo.InvariantCulture);

            var tf = CreateTypeface();
            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            _lineFt = new FormattedText(
                textToUse,
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                tf, (double)GetValue(TextBlock.FontSizeProperty),
                BackBrush, pixelsPerDip); 
            _plusMinusFt = new FormattedText(
                "+ ",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                tf, (double)GetValue(TextBlock.FontSizeProperty),
                BackBrush, pixelsPerDip);

            // NB: This is a bit tricky. We use the margin control to actually
            // draw the diff "+/-" prefix, so that it's not selectable. So, looking
            // at this from the perspective of a single line, the arrangement is:
            //
            // margin-lineFt-margin-lineFt-margin-margin-plusMinusFt
            return new Size(
                _lineFt.Width + _plusMinusFt.WidthIncludingTrailingWhitespace + (TextHorizontalMargin * 2.0),
                0.0);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (Lines == null || Lines.Count == 0) return;
            FormattedText _lineFt;
            var textToUse = Lines.Last().Number.ToString(CultureInfo.InvariantCulture);

            var tf = CreateTypeface();
            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            _lineFt = new FormattedText(
                textToUse,
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                tf, (double)GetValue(TextBlock.FontSizeProperty),
                BackBrush, pixelsPerDip);
            var lineNumberWidth = Math.Round(_lineFt.Width + TextHorizontalMargin * 2.0);

            var fontSize = (double)GetValue(TextBlock.FontSizeProperty);

            var visualLines = TextView.VisualLinesValid ? TextView.VisualLines : Enumerable.Empty<VisualLine>();
            foreach (var v in visualLines)
            {
                var rcs = BackgroundGeometryBuilder.GetRectsFromVisualSegment(TextView, v, 0, 1000).ToArray();
                var linenum = v.FirstDocumentLine.LineNumber - 1;
                if (linenum >= Lines.Count) continue;

                var diffLine = Lines[linenum];

                FormattedText ft;

                if (diffLine.Type !=  CompareAction.Blank)
                {
                    var brush = default(Brush);
                    switch (diffLine.Type)
                    {
                        case CompareAction.Added:
                            brush = AddedBackground;
                            break;
                        case CompareAction.Deleted:
                            brush = DeletedBackground;
                            break;
                        case CompareAction.None:
                            brush = BlankBackground;
                            break;
                    }

                    foreach (var rc in rcs)
                    {
                        drawingContext.DrawRectangle(brush, BorderlessPen, new Rect(0, rc.Top, ActualWidth, rc.Height));
                    }
                }

                if (diffLine.Type != CompareAction.Blank)
                {
                    ft = new FormattedText(diffLine.Number+"",
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        tf, fontSize, ForegroundBrush, pixelsPerDip);

                    var left = TextHorizontalMargin;

                    drawingContext.DrawText(ft, new Point(left, rcs[0].Top));
                }

                if (diffLine.Type != CompareAction.Blank)
                {
                    var prefix = "";
                    if (diffLine.Type == CompareAction.Added) prefix = "+ ";
                    else if (diffLine.Type == CompareAction.Deleted) prefix = "- ";
                    ft = new FormattedText(prefix,
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        tf, fontSize, (Brush)TextView.GetValue(Control.ForegroundProperty), pixelsPerDip);

                    drawingContext.DrawText(ft, new Point(lineNumberWidth + TextHorizontalMargin, rcs[0].Top));
                }
            }
        }

        public CompareDocuemnt Lines { get; set; }

        Typeface CreateTypeface()
        {
            var fe = TextView;
            return new Typeface((FontFamily)fe.GetValue(TextBlock.FontFamilyProperty),
                (FontStyle)fe.GetValue(TextBlock.FontStyleProperty),
                (FontWeight)fe.GetValue(TextBlock.FontWeightProperty),
                (FontStretch)fe.GetValue(TextBlock.FontStretchProperty));
        }
    }
}
