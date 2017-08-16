using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    public static class SymbolDisplayPartExtensions
    {
        private const string LeftToRightMarkerPrefix = "\u200e";
        
        public static string ToVisibleDisplayString(this TaggedText part, bool includeLeftToRightMarker)
        {
            var text = part.ToString();

            if (includeLeftToRightMarker)
            {
                if (part.Tag == TextTags.Punctuation ||
                    part.Tag == TextTags.Space ||
                    part.Tag == TextTags.LineBreak)
                {
                    text = LeftToRightMarkerPrefix + text;
                }
            }

            return text;
        }

        public static Run ToRun(this TaggedText text, bool isBold = false)
        {
            var s = text.ToVisibleDisplayString(includeLeftToRightMarker: true);

            var run = new Run(s);

            if (isBold)
            {
                run.FontWeight = FontWeights.Bold;
            }

            switch (text.Tag)
            {
                case TextTags.Keyword:
                    run.Foreground = Brushes.Blue;
                    break;
                case TextTags.Struct:
                case TextTags.Enum:
                case TextTags.TypeParameter:
                case TextTags.Class:
                case TextTags.Delegate:
                case TextTags.Interface:
                    run.Foreground = Brushes.Teal;
                    break;
            }

            return run;
        }

        public static TextBlock ToTextBlock(this IEnumerable<TaggedText> text, bool isBold = false)
        {
            var result = new TextBlock { TextWrapping = TextWrapping.Wrap };

            foreach (var part in text)
            {
                result.Inlines.Add(part.ToRun(isBold));
            }

            return result;
        }
    }
}