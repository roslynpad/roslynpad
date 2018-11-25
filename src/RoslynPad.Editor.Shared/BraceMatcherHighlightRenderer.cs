// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
#if AVALONIA
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CommonBrush = Avalonia.Media.IBrush;
#else
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using CommonBrush = System.Windows.Media.Brush;
#endif
using RoslynPad.Roslyn.BraceMatching;

namespace RoslynPad.Editor
{
    public class BraceMatcherHighlightRenderer : IBackgroundRenderer
    {
        private readonly TextView _textView;
        private readonly CommonBrush _backgroundBrush;

        public BraceMatchingResult? LeftOfPosition { get; private set; }
        public BraceMatchingResult? RightOfPosition { get; private set; }

        public const string BracketHighlight = "Bracket highlight";

        public BraceMatcherHighlightRenderer(TextView textView, IClassificationHighlightColors classificationHighlightColors)
        {
            _textView = textView ?? throw new ArgumentNullException(nameof(textView));

            _textView.BackgroundRenderers.Add(this);

            var brush = classificationHighlightColors
                .GetBrush(ClassificationHighlightColors.BraceMatchingClassificationTypeName)
                ?.Background?.GetBrush(null);

            if (brush != null)
            {
                _backgroundBrush = brush;
            }
            else
            {
                _backgroundBrush = Brushes.Transparent;
            }
        }

        public void SetHighlight(BraceMatchingResult? leftOfPosition, BraceMatchingResult? rightOfPosition)
        {
            if (LeftOfPosition != leftOfPosition || RightOfPosition != rightOfPosition)
            {
                LeftOfPosition = leftOfPosition;
                RightOfPosition = rightOfPosition;
                _textView.InvalidateLayer(Layer);
            }
        }

        public KnownLayer Layer => KnownLayer.Selection;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (LeftOfPosition == null && RightOfPosition == null)
                return;

            var builder = new BackgroundGeometryBuilder
            {
                CornerRadius = 1,
#if !AVALONIA
                AlignToWholePixels = true
#endif
            };

            if (RightOfPosition != null)
            {
                builder.AddSegment(textView, new TextSegment { StartOffset = RightOfPosition.Value.LeftSpan.Start, Length = RightOfPosition.Value.LeftSpan.Length });
                builder.CloseFigure();
                builder.AddSegment(textView, new TextSegment { StartOffset = RightOfPosition.Value.RightSpan.Start, Length = RightOfPosition.Value.RightSpan.Length });
                builder.CloseFigure();
            }

            if (LeftOfPosition != null)
            {
                builder.AddSegment(textView, new TextSegment { StartOffset = LeftOfPosition.Value.LeftSpan.Start, Length = LeftOfPosition.Value.LeftSpan.Length });
                builder.CloseFigure();
                builder.AddSegment(textView, new TextSegment { StartOffset = LeftOfPosition.Value.RightSpan.Start, Length = LeftOfPosition.Value.RightSpan.Length });
                builder.CloseFigure();
            }

            var geometry = builder.CreateGeometry();
            if (geometry != null)
            {
                drawingContext.DrawGeometry(_backgroundBrush, null, geometry);
            }
        }
    }
}
