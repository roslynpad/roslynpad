#nullable enable

namespace Microsoft.VisualStudio.Text.Formatting.Implementation;

using System.Collections.ObjectModel;

using Avalonia.Media;
using Avalonia.Media.TextFormatting;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Projection;

/// <summary>
/// Formats snapshot lines into <see cref="FormattedLine"/> rows with Avalonia's
/// <see cref="TextFormatter"/>, using the view's classifier and classification format map to
/// build the runs. An instance is a pure function of (snapshot, format map, options); the
/// view replaces it whenever any of those change.
/// </summary>
internal sealed class FormattedLineSource : IFormattedLineSource
{
    private readonly IClassifier? _classifier;
    private readonly IClassificationFormatMap _classificationFormatMap;
    private readonly IBufferGraph? _bufferGraph;
    private readonly ITextAndAdornmentSequencer? _sequencer;
    private readonly TextFormatter _formatter;
    private readonly EditorTextParagraphProperties _paragraphProperties;
    private readonly double _defaultBaseline;
    private readonly double _defaultTextHeight;

    public FormattedLineSource(
        ITextSnapshot sourceTextSnapshot,
        ITextSnapshot topTextSnapshot,
        IClassifier? classifier,
        IClassificationFormatMap classificationFormatMap,
        int tabSize,
        double baseIndentation,
        double wordWrapWidth,
        double maxAutoIndent,
        bool useDisplayMode,
        IBufferGraph? bufferGraph,
        ITextAndAdornmentSequencer? sequencer = null)
    {
        _sequencer = sequencer;
        ArgumentNullException.ThrowIfNull(sourceTextSnapshot);
        ArgumentNullException.ThrowIfNull(topTextSnapshot);
        ArgumentNullException.ThrowIfNull(classificationFormatMap);
        ArgumentOutOfRangeException.ThrowIfLessThan(tabSize, 1);

        SourceTextSnapshot = sourceTextSnapshot;
        TopTextSnapshot = topTextSnapshot;
        _classifier = classifier;
        _classificationFormatMap = classificationFormatMap;
        TabSize = tabSize;
        BaseIndentation = baseIndentation;
        MaxAutoIndent = maxAutoIndent;
        UseDisplayMode = useDisplayMode;
        _bufferGraph = bufferGraph;
        _formatter = TextFormatter.Current;

        DefaultTextProperties = classificationFormatMap.DefaultTextProperties;

        // Nominal metrics come from a single space formatted with the default properties.
        var metricsSource = new ClassifiedTextSource(" ".AsMemory(), [new ClassifiedTextSource.FormattingRun(0, 1, DefaultTextProperties)]);
        var metricsParagraph = new EditorTextParagraphProperties(DefaultTextProperties, TextWrapping.NoWrap, 0.0, 0.0);
        var metricsLine = _formatter.FormatLine(metricsSource, 0, double.PositiveInfinity, metricsParagraph)
            ?? throw new InvalidOperationException("The text formatter failed to produce a line.");
        ColumnWidth = metricsLine.WidthIncludingTrailingWhitespace;
        LineHeight = metricsLine.Height;
        TextHeightAboveBaseline = metricsLine.Baseline;
        TextHeightBelowBaseline = metricsLine.Height - metricsLine.Baseline;

        // Word wrap cannot be narrower than a few columns, or formatting cannot make progress.
        WordWrapWidth = wordWrapWidth <= 0.0 ? 0.0 : Math.Max(wordWrapWidth, 4.0 * ColumnWidth);

        _paragraphProperties = new EditorTextParagraphProperties(
            DefaultTextProperties,
            WordWrapWidth > 0.0 ? TextWrapping.WrapWithOverflow : TextWrapping.NoWrap,
            defaultIncrementalTab: TabSize * ColumnWidth,
            indent: 0.0);
        _defaultBaseline = TextHeightAboveBaseline;
        _defaultTextHeight = LineHeight;
    }

    public ITextSnapshot SourceTextSnapshot { get; }

    public ITextSnapshot TopTextSnapshot { get; }

    public bool UseDisplayMode { get; }

    public int TabSize { get; }

    public double BaseIndentation { get; }

    public double WordWrapWidth { get; }

    public double MaxAutoIndent { get; }

    public double ColumnWidth { get; }

    public double LineHeight { get; }

    public double TextHeightAboveBaseline { get; }

    public double TextHeightBelowBaseline { get; }

    public TextRunProperties DefaultTextProperties { get; }

    public Collection<IFormattedLine> FormatLineInVisualBuffer(ITextSnapshotLine visualLine)
    {
        ArgumentNullException.ThrowIfNull(visualLine);
        if (visualLine.Snapshot != TopTextSnapshot)
        {
            throw new ArgumentException("The line belongs to a different snapshot.");
        }

        string text = visualLine.GetText();
        var runs = BuildRuns(visualLine, text.Length);
        var adornments = BuildAdornments(visualLine, text.Length, out var embeddedRuns, out var embeddedSpans);
        BuildTabRuns(text, runs, ref embeddedRuns, ref embeddedSpans);
        var source = new ClassifiedTextSource(text.AsMemory(), runs, embeddedRuns, embeddedSpans);
        bool endsAtEndOfBuffer = visualLine.LineNumber == TopTextSnapshot.LineCount - 1;
        double paragraphWidth = WordWrapWidth > 0.0 ? Math.Max(ColumnWidth, WordWrapWidth - BaseIndentation) : double.PositiveInfinity;

        var result = new Collection<IFormattedLine>();
        int index = 0;
        TextLineBreak? previousBreak = null;
        while (true)
        {
            var textLine = _formatter.FormatLine(source, index, paragraphWidth, _paragraphProperties, previousBreak);
            if (textLine is null)
            {
                break;
            }

            int rowLength = Math.Min(textLine.Length, text.Length - index);
            bool isLastRow = index + rowLength >= text.Length;
            result.Add(new FormattedLine(
                textLine,
                runs,
                adornments,
                SourceTextSnapshot,
                TopTextSnapshot,
                _bufferGraph,
                paragraphStart: visualLine.Start.Position,
                rowStart: index,
                rowLength: rowLength,
                lineBreakLength: isLastRow ? visualLine.LineBreakLength : 0,
                isFirstRow: index == 0,
                isLastRow: isLastRow,
                endsAtEndOfBuffer: endsAtEndOfBuffer,
                left: BaseIndentation,
                columnWidth: ColumnWidth,
                defaultBaseline: _defaultBaseline,
                defaultTextHeight: _defaultTextHeight));

            index += rowLength;
            previousBreak = textLine.TextLineBreak;
            if (index >= text.Length || rowLength == 0)
            {
                break;
            }
        }

        if (result.Count == 0)
        {
            // Empty line: format a nominal-metrics row of zero length.
            var emptyLine = _formatter.FormatLine(source, 0, paragraphWidth, _paragraphProperties)
                ?? _formatter.FormatLine(
                    new ClassifiedTextSource(" ".AsMemory(), [new ClassifiedTextSource.FormattingRun(0, 1, DefaultTextProperties)]),
                    0,
                    double.PositiveInfinity,
                    _paragraphProperties)!;
            result.Add(new FormattedLine(
                emptyLine,
                runs,
                adornments,
                SourceTextSnapshot,
                TopTextSnapshot,
                _bufferGraph,
                paragraphStart: visualLine.Start.Position,
                rowStart: 0,
                rowLength: 0,
                lineBreakLength: visualLine.LineBreakLength,
                isFirstRow: true,
                isLastRow: true,
                endsAtEndOfBuffer: endsAtEndOfBuffer,
                left: BaseIndentation,
                columnWidth: ColumnWidth,
                defaultBaseline: _defaultBaseline,
                defaultTextHeight: _defaultTextHeight));
        }

        return result;
    }

    /// <summary>
    /// Builds the line's space-negotiating adornments as embedded runs: each
    /// adornment that replaces a non-empty span within the line becomes a drawable run
    /// consuming that span. Zero-length (purely inserted) adornments are not yet
    /// negotiated — documented divergence.
    /// </summary>
    private List<FormattedLine.AdornmentInfo> BuildAdornments(
        ITextSnapshotLine visualLine,
        int lineLength,
        out List<DrawableTextRun>? embeddedRuns,
        out List<(int Start, int End)>? embeddedSpans)
    {
        embeddedRuns = null;
        embeddedSpans = null;
        var infos = new List<FormattedLine.AdornmentInfo>();
        if (_sequencer is null || lineLength == 0)
        {
            return infos;
        }

        int lineStart = visualLine.Start.Position;
        foreach (var element in _sequencer.CreateTextAndAdornmentCollection(visualLine, SourceTextSnapshot))
        {
            if (element is not IAdornmentElement adornment)
            {
                continue;
            }

            // The sequencer's spans map to any buffer in the view's graph; visual-snapshot
            // spans give line-relative offsets directly (also correct under elision).
            foreach (var span in adornment.Span.GetSpans(TopTextSnapshot))
            {
                int start = span.Start.Position - lineStart;
                int end = span.End.Position - lineStart;
                if (end <= start || start < 0 || end > lineLength)
                {
                    continue;
                }

                infos.Add(new FormattedLine.AdornmentInfo(start, end, adornment));
                embeddedRuns ??= [];
                embeddedSpans ??= [];
                embeddedRuns.Add(new EmbeddedAdornmentRun(end - start, adornment, DefaultTextProperties));
                embeddedSpans.Add((start, end));
            }
        }

        return infos;
    }

    /// <summary>
    /// Implements VS tab semantics on top of Avalonia's formatter (which only offers a
    /// fixed incremental tab): every tab outside an adornment-replaced span becomes a
    /// drawable run whose width reaches the next multiple of TabSize * ColumnWidth from
    /// the line's start. Text between tabs is measured by shaping it in isolation (tabs
    /// break shaping runs in any engine, so this is exact for the segment); the common
    /// leading-tabs case needs no extra shaping at all.
    /// </summary>
    /// <remarks>Known divergences: a tab inside a bidi run reordered across the tab, and
    /// tabs on wrapped rows (stops computed from the paragraph start, not the row start),
    /// follow this seam's logical-order model rather than WPF's visual-order one.</remarks>
    private void BuildTabRuns(
        string text,
        List<ClassifiedTextSource.FormattingRun> runs,
        ref List<DrawableTextRun>? embeddedRuns,
        ref List<(int Start, int End)>? embeddedSpans)
    {
        int firstTab = text.IndexOf('\t', StringComparison.Ordinal);
        if (firstTab < 0)
        {
            return;
        }

        double tabWidth = TabSize * ColumnWidth;
        var adornmentSpans = embeddedSpans;
        double x = 0.0;
        int segmentStart = 0;
        for (int i = firstTab; i < text.Length; i++)
        {
            if (text[i] != '\t')
            {
                continue;
            }

            // Tabs consumed by an adornment's replaced span belong to the adornment.
            if (adornmentSpans is not null && adornmentSpans.Any(span => i >= span.Start && i < span.End))
            {
                continue;
            }

            x += MeasureRange(text, segmentStart, i, runs, adornmentSpans, embeddedRuns);
            double advance = tabWidth - (x - (Math.Floor(x / tabWidth) * tabWidth));
            embeddedRuns ??= [];
            embeddedSpans ??= adornmentSpans = [];
            embeddedRuns.Add(new TabRun(advance, _defaultTextHeight, _defaultBaseline, PropertiesAt(runs, i)));
            embeddedSpans.Add((i, i + 1));
            x += advance;
            segmentStart = i + 1;
        }
    }

    /// <summary>Measures [start, end) of the line: shaped text chunks plus the widths of
    /// any adornment-replaced spans inside the range.</summary>
    private double MeasureRange(
        string text,
        int start,
        int end,
        List<ClassifiedTextSource.FormattingRun> runs,
        List<(int Start, int End)>? adornmentSpans,
        List<DrawableTextRun>? embeddedRuns)
    {
        double width = 0.0;
        int current = start;
        while (current < end)
        {
            int chunkEnd = end;
            if (adornmentSpans is not null)
            {
                for (int i = 0; i < adornmentSpans.Count; i++)
                {
                    var (spanStart, spanEnd) = adornmentSpans[i];
                    if (current >= spanStart && current < spanEnd)
                    {
                        width += embeddedRuns![i].Size.Width;
                        current = spanEnd;
                        chunkEnd = -1;
                        break;
                    }

                    if (spanStart > current && spanStart < chunkEnd)
                    {
                        chunkEnd = spanStart;
                    }
                }
            }

            if (chunkEnd < 0)
            {
                continue;
            }

            width += MeasureText(text, current, chunkEnd, runs);
            current = chunkEnd;
        }

        return width;
    }

    private double MeasureText(string text, int start, int end, List<ClassifiedTextSource.FormattingRun> runs)
    {
        if (end <= start)
        {
            return 0.0;
        }

        var chunkRuns = new List<ClassifiedTextSource.FormattingRun>();
        foreach (var run in runs)
        {
            if (run.End > start && run.Start < end)
            {
                chunkRuns.Add(new ClassifiedTextSource.FormattingRun(
                    Math.Max(run.Start, start) - start,
                    Math.Min(run.End, end) - start,
                    run.Properties));
            }
        }

        var line = _formatter.FormatLine(
            new ClassifiedTextSource(text.AsMemory(start, end - start), chunkRuns),
            0,
            double.PositiveInfinity,
            new EditorTextParagraphProperties(DefaultTextProperties, TextWrapping.NoWrap, TabSize * ColumnWidth, 0.0));
        return line?.WidthIncludingTrailingWhitespace ?? 0.0;
    }

    private TextRunProperties PropertiesAt(List<ClassifiedTextSource.FormattingRun> runs, int index)
    {
        foreach (var run in runs)
        {
            if (index >= run.Start && index < run.End)
            {
                return run.Properties;
            }
        }

        return DefaultTextProperties;
    }

    /// <summary>
    /// Builds a dense list of formatting runs covering [0, lineLength): classified spans map
    /// through the classification format map, gaps get the default properties.
    /// </summary>
    private List<ClassifiedTextSource.FormattingRun> BuildRuns(ITextSnapshotLine visualLine, int lineLength)
    {
        var runs = new List<ClassifiedTextSource.FormattingRun>();
        if (lineLength == 0)
        {
            return runs;
        }

        int current = 0;
        if (_classifier is not null)
        {
            // The classifier works on the edit buffer. Identity models classify the line
            // span directly; under elision each visible edit segment is classified and its
            // runs land at the segment's visual offset (hidden text has no runs).
            if (ReferenceEquals(SourceTextSnapshot, TopTextSnapshot))
            {
                AddClassifiedRuns(runs, visualLine.Extent, visualLine.Start.Position, lineLength, ref current);
            }
            else if (_bufferGraph is not null)
            {
                int visualOffset = 0;
                foreach (var editSpan in _bufferGraph.MapDownToSnapshot(visualLine.Extent, SpanTrackingMode.EdgeExclusive, SourceTextSnapshot))
                {
                    AddClassifiedRuns(runs, editSpan, editSpan.Start.Position - visualOffset, visualOffset + editSpan.Length, ref current);
                    visualOffset += editSpan.Length;
                }
            }
        }

        if (current < lineLength)
        {
            runs.Add(new ClassifiedTextSource.FormattingRun(current, lineLength, DefaultTextProperties));
        }

        return runs;
    }

    /// <summary>Classifies <paramref name="span"/> and appends its runs; classified edit
    /// positions convert to line-relative visual offsets by subtracting
    /// <paramref name="editToVisualDelta"/>, clipped to <paramref name="visualLimit"/>.</summary>
    private void AddClassifiedRuns(
        List<ClassifiedTextSource.FormattingRun> runs,
        SnapshotSpan span,
        int editToVisualDelta,
        int visualLimit,
        ref int current)
    {
        foreach (var classificationSpan in _classifier!.GetClassificationSpans(span))
        {
            int spanStart = Math.Max(0, classificationSpan.Span.Start.Position - editToVisualDelta);
            int spanEnd = Math.Min(visualLimit, classificationSpan.Span.End.Position - editToVisualDelta);
            if (spanEnd <= current)
            {
                continue;
            }

            if (spanStart > current)
            {
                runs.Add(new ClassifiedTextSource.FormattingRun(current, spanStart, DefaultTextProperties));
            }

            var properties = _classificationFormatMap.GetTextProperties(classificationSpan.ClassificationType);
            runs.Add(new ClassifiedTextSource.FormattingRun(Math.Max(spanStart, current), spanEnd, properties));
            current = spanEnd;
        }
    }
}
