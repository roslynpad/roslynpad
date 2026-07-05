#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using Avalonia.Media.TextFormatting;

/// <summary>
/// An Avalonia text source over the text of one snapshot line (exclusive of its line break),
/// producing one run per classification span. Text source indices are relative to the start
/// of the snapshot line.
/// </summary>
internal sealed class ClassifiedTextSource : ITextSource
{
    private readonly ReadOnlyMemory<char> _text;
    private readonly IReadOnlyList<FormattingRun> _runs;
    private readonly IReadOnlyList<DrawableTextRun>? _adornments;
    private readonly IReadOnlyList<(int Start, int End)>? _adornmentSpans;

    public ClassifiedTextSource(
        ReadOnlyMemory<char> text,
        IReadOnlyList<FormattingRun> runs,
        IReadOnlyList<DrawableTextRun>? adornments = null,
        IReadOnlyList<(int Start, int End)>? adornmentSpans = null)
    {
        _text = text;
        _runs = runs;
        _adornments = adornments;
        _adornmentSpans = adornmentSpans;
    }

    public TextRun? GetTextRun(int textSourceIndex)
    {
        if (textSourceIndex >= _text.Length)
        {
            return null;
        }

        if (_adornments is not null && _adornmentSpans is not null)
        {
            for (int i = 0; i < _adornmentSpans.Count; i++)
            {
                var (start, end) = _adornmentSpans[i];
                if (textSourceIndex == start)
                {
                    return _adornments[i];
                }

                // Inside a replaced span: skip to its end by re-entering there.
                if (textSourceIndex > start && textSourceIndex < end)
                {
                    return GetTextRun(end);
                }
            }
        }

        foreach (var run in _runs)
        {
            if (textSourceIndex < run.End)
            {
                int start = Math.Max(textSourceIndex, run.Start);
                int end = run.End;
                if (_adornmentSpans is not null)
                {
                    foreach (var (adornmentStart, _) in _adornmentSpans)
                    {
                        if (adornmentStart > start && adornmentStart < end)
                        {
                            end = adornmentStart;
                        }
                    }
                }

                return new TextCharacters(_text[start..end], run.Properties);
            }
        }

        return null;
    }

    /// <summary>A classified run: [Start, End) in line-relative coordinates.</summary>
    internal readonly record struct FormattingRun(int Start, int End, TextRunProperties Properties);
}
