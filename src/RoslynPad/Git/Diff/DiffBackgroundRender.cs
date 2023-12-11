using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit.Rendering;

namespace RoslynPad
{
    public class DiffLineBackgroundRenderer : IBackgroundRenderer
    {
        static readonly Brush AddedBackground;
        static readonly Brush DeletedBackground;
        static readonly Brush NormalBackground;
        static readonly ImageBrush BlankBackground;

        static readonly Pen BorderlessPen;

        static DiffLineBackgroundRenderer()
        {
            AddedBackground = new SolidColorBrush(Color.FromRgb(0xdd, 0xff, 0xdd));
            AddedBackground.Freeze();

            DeletedBackground = new SolidColorBrush(Color.FromRgb(0xff, 0xdd, 0xdd));
            DeletedBackground.Freeze();

            NormalBackground = new SolidColorBrush(Color.FromRgb(0xfa, 0xfa, 0xfa));
            NormalBackground.Freeze();

            var transparentBrush = new SolidColorBrush(Colors.Transparent);
            transparentBrush.Freeze();

            BorderlessPen = new Pen(transparentBrush, 0.0);
            BorderlessPen.Freeze(); 
            BlankBackground = new ImageBrush();
            BlankBackground.ImageSource =
                new BitmapImage(new Uri(@"pack://application:,,,/Resources/blank.png", UriKind.RelativeOrAbsolute));
            BlankBackground.TileMode = TileMode.Tile;
            BlankBackground.ViewportUnits = BrushMappingMode.Absolute;
            BlankBackground.Viewport = new Rect(0, 0, 10, 10);
            BlankBackground.Stretch = Stretch.None;
            BlankBackground.Freeze();
        }
        public DiffLineBackgroundRenderer(CompareDocuemnt doc)
        {
            Lines = doc;
        }
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (Lines == null) return;

            foreach (var v in textView.VisualLines)
            {
                var linenum = v.FirstDocumentLine.LineNumber - 1;
                if (linenum >= Lines.Count) continue;

                var diffLine = Lines[linenum];

                //if (diffLine.Type == CompareAction.Blank) continue;

                Brush brush = BlankBackground;
                switch (diffLine.Type)
                {
                    case CompareAction.Added:
                        brush = AddedBackground;
                        break;
                    case CompareAction.Deleted:
                        brush = DeletedBackground;
                        break;
                    case CompareAction.None:
                        brush = NormalBackground;
                        break;
                }

                foreach (var rc in BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, v, 0, 1000))
                {
                    drawingContext.DrawRectangle(brush, BorderlessPen,
                        new Rect(0, rc.Top, textView.ActualWidth, rc.Height));
                }

            }

        }

        public KnownLayer Layer { get { return KnownLayer.Background; } }
        public CompareDocuemnt Lines { get; set; }
    }
}