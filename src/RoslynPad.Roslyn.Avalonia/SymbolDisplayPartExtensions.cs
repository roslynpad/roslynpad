using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.CodeAnalysis;

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

    private static IBrush _lightKeywordBrush = Brushes.Blue;
    private static IBrush _lightTypeBrush = Brushes.Teal;

    private static IBrush _darkKeywordBrush = new SolidColorBrush(Color.FromRgb(86, 156, 214));
    private static IBrush _darkTypeBrush = new SolidColorBrush(Color.FromRgb(78, 201, 176));

    private static IBrush _currentKeywordBrush = _lightKeywordBrush;
    private static IBrush _currentTypeBrush = _lightTypeBrush;

    public static void SetTheme(bool isLightTheme)
    {
        if (isLightTheme)
        {
            _currentKeywordBrush = _lightKeywordBrush;
            _currentTypeBrush = _lightTypeBrush;
        }
        else
        {
            _currentKeywordBrush = _darkKeywordBrush;
            _currentTypeBrush = _darkTypeBrush;
        }
    }

    public static TextBlock ToRun(this TaggedText text, bool isBold = false)
    {
        var s = text.ToVisibleDisplayString(includeLeftToRightMarker: false);

        var run = new TextBlock { Text = s };

        if (isBold)
        {
            run.FontWeight = FontWeight.Bold;
        }

        switch (text.Tag)
        {
            case TextTags.Keyword:
                run.Foreground = _currentKeywordBrush;
                break;
            case TextTags.Struct:
            case TextTags.Enum:
            case TextTags.TypeParameter:
            case TextTags.Class:
            case TextTags.Delegate:
            case TextTags.Interface:
                run.Foreground = _currentTypeBrush;
                break;
        }

        return run;
    }

    public static Panel ToTextBlock(this IEnumerable<TaggedText> text, bool isBold = false)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };

        foreach (var part in text)
        {
            panel.Children.Add(part.ToRun(isBold));
        }

        return panel;
    }
}
