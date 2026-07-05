#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using Avalonia.Media;
using Avalonia.Media.TextFormatting;

/// <summary>
/// Paragraph properties for editor lines: left-aligned, wrapping controlled by the line
/// source, tab stops every <c>TabSize</c> columns. The paragraph flow direction is always
/// left-to-right per the VS view model; right-to-left text is handled by bidi runs within
/// the line, with bidi treated as a first-class concern.
/// </summary>
internal sealed class EditorTextParagraphProperties : TextParagraphProperties
{
    private readonly TextRunProperties _defaultTextRunProperties;
    private readonly TextWrapping _textWrapping;
    private readonly double _defaultIncrementalTab;
    private readonly double _indent;

    public EditorTextParagraphProperties(
        TextRunProperties defaultTextRunProperties,
        TextWrapping textWrapping,
        double defaultIncrementalTab,
        double indent)
    {
        _defaultTextRunProperties = defaultTextRunProperties;
        _textWrapping = textWrapping;
        _defaultIncrementalTab = defaultIncrementalTab;
        _indent = indent;
    }

    public override FlowDirection FlowDirection => FlowDirection.LeftToRight;

    public override TextAlignment TextAlignment => TextAlignment.Left;

    public override double LineHeight => double.NaN;

    public override bool FirstLineInParagraph => true;

    public override TextRunProperties DefaultTextRunProperties => _defaultTextRunProperties;

    public override TextWrapping TextWrapping => _textWrapping;

    public override double DefaultIncrementalTab => _defaultIncrementalTab;

    public override double Indent => _indent;
}
