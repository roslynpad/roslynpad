using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
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

        var s = text.ToVisibleDisplayString(includeLeftToRightMarker: false);

        var run = new Run(s);

        var style = text.GetStyle();
        if (isBold || style.HasFlag(TaggedTextStyle.Strong))
        {
            run.FontWeight = FontWeight.Bold;
        }

        if (style.HasFlag(TaggedTextStyle.Emphasis))
        {
            run.FontStyle = FontStyle.Italic;
        }

        if (style.HasFlag(TaggedTextStyle.Underline))
        {
            run.TextDecorations = TextDecorations.Underline;
        }

        run[!TextBlock.ForegroundProperty] = new DynamicResourceExtension(TaggedTextResources.GetResourceKey(text.Tag));

        return run;
    }

    public static TextBlock ToTextBlock(this IEnumerable<TaggedText> text, bool isBold = false)
    {
        InlineCollection? inlines = null;
        foreach (var part in text)
        {
            inlines ??= [];
            inlines.Add(part.ToInline(isBold));
        }

        return new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Inlines = inlines,
            IsVisible = inlines is not null
        };
    }
}