using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
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

        public static TextBlock ToTextBlock(this TaggedText text)
        {
            var s = text.ToVisibleDisplayString(includeLeftToRightMarker: true);

            var run = new TextBlock { Text = s };

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

        public static Control ToTextControl(this IEnumerable<TaggedText> text)
        {
            var panel = new WrapPanel { Orientation = Orientation.Horizontal };

            foreach (var part in text)
            {
                panel.Children.Add(part.ToTextBlock());
            }

            return panel;
        }
    }
}