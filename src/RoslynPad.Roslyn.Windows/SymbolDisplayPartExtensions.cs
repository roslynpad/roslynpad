using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn.Classification;

namespace RoslynPad.Roslyn;

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

    public static Inline ToInline(this TaggedText text, bool isBold = false)
    {
        if (text.Tag == TextTags.LineBreak)
        {
            return new LineBreak();
        }

        var s = text.ToVisibleDisplayString(includeLeftToRightMarker: true);

        var run = new Run(s);

        var style = text.GetStyle();
        if (isBold || style.HasFlag(TaggedTextStyle.Strong))
        {
            run.FontWeight = FontWeights.Bold;
        }

        if (style.HasFlag(TaggedTextStyle.Emphasis))
        {
            run.FontStyle = FontStyles.Italic;
        }

        if (style.HasFlag(TaggedTextStyle.Underline))
        {
            run.TextDecorations = TextDecorations.Underline;
        }

        run.SetResourceReference(TextElement.ForegroundProperty, TaggedTextResources.GetResourceKey(text.Tag));

        return run;
    }

    public static TextBlock ToTextBlock(this IEnumerable<TaggedText> text, bool isBold = false)
    {
        var result = new TextBlock { TextWrapping = TextWrapping.Wrap };

        foreach (var part in text)
        {
            result.Inlines.Add(part.ToInline(isBold));
        }

        return result;
    }
}