using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Formatting
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

            //var format = formatMap.GetTextProperties(typeMap.GetClassificationType(part.Kind.ToClassificationTypeName()));
            //run.SetTextProperties(format);

            return run;
        }

        public static TextBlock ToTextBlock(this ImmutableArray<SymbolDisplayPart> parts)
        {
            return parts.AsEnumerable().ToTextBlock();
        }

        public static TextBlock ToTextBlock(this IEnumerable<SymbolDisplayPart> parts)
        {
            var result = new TextBlock();

            //var formatMap = typeMap.ClassificationFormatMapService.GetClassificationFormatMap("tooltip");
            //result.SetDefaultTextProperties(formatMap);

            foreach (var part in parts)
            {
                result.Inlines.Add(part.ToRun());
            }

            return result;
        }

    }
}