using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    public static class SymbolDisplayPartExtensions
    {
        private const string LeftToRightMarkerPrefix = "\u200e";

        public static string ToVisibleDisplayString(this SymbolDisplayPart part, bool includeLeftToRightMarker)
        {
            var text = part.ToString();

            if (includeLeftToRightMarker)
            {
                if (part.Kind == SymbolDisplayPartKind.Punctuation ||
                    part.Kind == SymbolDisplayPartKind.Space ||
                    part.Kind == SymbolDisplayPartKind.LineBreak)
                {
                    text = LeftToRightMarkerPrefix + text;
                }
            }

            return text;
        }

        public static string ToVisibleDisplayString(this IEnumerable<SymbolDisplayPart> parts, bool includeLeftToRightMarker)
        {
            return string.Join(string.Empty, parts.Select(p => p.ToVisibleDisplayString(includeLeftToRightMarker)));
        }

        public static Run ToRun(this SymbolDisplayPart part)
        {
            var text = part.ToVisibleDisplayString(includeLeftToRightMarker: true);

            var run = new Run(text);

            switch (part.Kind)
            {
                case SymbolDisplayPartKind.Keyword:
                    run.Foreground = Brushes.Blue;
                    break;
                case SymbolDisplayPartKind.StructName:
                case SymbolDisplayPartKind.EnumName:
                case SymbolDisplayPartKind.TypeParameterName:
                case SymbolDisplayPartKind.ClassName:
                case SymbolDisplayPartKind.DelegateName:
                case SymbolDisplayPartKind.InterfaceName:
                    run.Foreground = Brushes.Teal;
                    break;
            }

            return run;
        }

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

        public static Run ToRun(this TaggedText text)
        {
            var s = text.ToVisibleDisplayString(includeLeftToRightMarker: true);

            var run = new Run(s);

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

        public static TextBlock ToTextBlock(this ImmutableArray<TaggedText> text)
        {
            var result = new TextBlock();

            foreach (var part in text)
            {
                result.Inlines.Add(part.ToRun());
            }

            return result;
        }

        public static TextBlock ToTextBlock(this ImmutableArray<SymbolDisplayPart> parts)
        {
            return parts.AsEnumerable().ToTextBlock();
        }

        public static TextBlock ToTextBlock(this IEnumerable<SymbolDisplayPart> parts)
        {
            var result = new TextBlock();
            
            foreach (var part in parts)
            {
                result.Inlines.Add(part.ToRun());
            }

            return result;
        }
    }
}