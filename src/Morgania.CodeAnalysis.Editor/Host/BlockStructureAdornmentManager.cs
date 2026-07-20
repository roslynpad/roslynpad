using System.Composition;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// The editor-format-map key and property names the block structure guides read their color
/// from. Hosts theme the guides by setting the property (as an <see cref="IBrush"/>) on the
/// map returned by <see cref="IEditorFormatMapService"/>; left unset, a translucent gray is
/// used.
/// </summary>
public static class BlockStructureFormatNames
{
    /// <summary>The editor format map key.</summary>
    public const string Name = "Block Structure Guide";

    /// <summary>The guide line's stroke brush.</summary>
    public const string Foreground = "Foreground";
}

/// <summary>
/// Draws vertical block structure guide lines from <see cref="IStructureTag"/>s (produced by
/// Roslyn's structure tagger) into the view's predefined BlockStructure adornment layer. The
/// VS implementation of this manager lives in the closed-source editor; this one derives the
/// guide from the tag spans per the <see cref="IStructureTag"/> contract: anchored at the
/// first non-whitespace character of the header's start line, spanning from below the header
/// line to the end of the outlining span, drawn in segments that skip lines with text at the
/// guide's column (which naturally clips the guide at the block's braces).
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[Shared]
[ContentType("Roslyn Languages")]
[TextViewRole(PredefinedTextViewRoles.Document)]
internal sealed class BlockStructureAdornmentManagerProvider : IWpfTextViewCreationListener
{
    private readonly IViewTagAggregatorFactoryService _aggregatorFactory;
    private readonly IEditorFormatMapService _formatMapService;

    [ImportingConstructor]
    public BlockStructureAdornmentManagerProvider(
        IViewTagAggregatorFactoryService aggregatorFactory,
        IEditorFormatMapService formatMapService)
    {
        _aggregatorFactory = aggregatorFactory;
        _formatMapService = formatMapService;
    }

    public void TextViewCreated(IWpfTextView textView) =>
        _ = new BlockStructureAdornmentManager(
            textView,
            _aggregatorFactory.CreateTagAggregator<IStructureTag>(textView),
            _formatMapService.GetEditorFormatMap(textView));
}

internal sealed class BlockStructureAdornmentManager
{
    // MinLineHeight must always be larger than ContinuationPadding so that no segments are
    // created for vertical spans between lines (same constants as the VS structure visualizer
    // fork in Roslyn's string-indentation adornments).
    private const double MinLineHeight = 2.1;
    private const double ContinuationPadding = 2.0;

    private static readonly IBrush s_fallbackBrush = new SolidColorBrush(Color.FromArgb(0x30, 0x88, 0x88, 0x88));

    private readonly IWpfTextView _view;
    private readonly ITagAggregator<IStructureTag> _aggregator;
    private readonly IEditorFormatMap _formatMap;
    private readonly IAdornmentLayer _layer;

    public BlockStructureAdornmentManager(IWpfTextView view, ITagAggregator<IStructureTag> aggregator, IEditorFormatMap formatMap)
    {
        _view = view;
        _aggregator = aggregator;
        _formatMap = formatMap;
        _layer = view.GetAdornmentLayer(PredefinedAdornmentLayers.BlockStructure);

        view.LayoutChanged += (_, _) => Redraw();
        // Tag changes can be raised from tagger worker threads.
        aggregator.BatchedTagsChanged += (_, _) => Dispatcher.UIThread.Post(Redraw);
        formatMap.FormatMappingChanged += (_, _) => Redraw();
        view.Closed += (_, _) => aggregator.Dispose();
    }

    private void Redraw()
    {
        if (_view.IsClosed || _view.InLayout || _view.TextViewLines is not { } lines)
        {
            return;
        }

        _layer.RemoveAllAdornments();

        var brush = GetBrush();
        var snapshot = _view.TextSnapshot;
        // The brush is translucent; coinciding guides from overlapping blocks would double up.
        var drawn = new HashSet<(double X, double Top, double Bottom)>();

        foreach (var mappingTag in _aggregator.GetTags(lines.FormattedSpan))
        {
            var tag = mappingTag.Tag;
            if (tag.Type == PredefinedStructureTagTypes.Nonstructural)
            {
                continue;
            }

            var headerSpan = Translate(tag.HeaderSpan, tag.Snapshot);
            var guideSpan = Translate(tag.GuideLineSpan, tag.Snapshot)
                ?? InferGuideSpan(headerSpan, Translate(tag.OutliningSpan, tag.Snapshot));
            if (guideSpan is not { } guide || GetAnchorPoint(tag, headerSpan) is not { } anchorPoint)
            {
                continue;
            }

            var guideStart = new SnapshotPoint(snapshot, guide.Start);
            var guideEnd = new SnapshotPoint(snapshot, guide.End);
            if (guideStart.GetContainingLineNumber() == guideEnd.GetContainingLineNumber()
                || guide.Start > lines.LastVisibleLine.End
                || guide.End < lines.FirstVisibleLine.Start)
            {
                continue;
            }

            // The view formats the anchor's line transiently when it is scrolled out, so
            // the x is always measurable.
            var anchor = new SnapshotPoint(snapshot, anchorPoint);
            var x = Math.Floor(_view.GetTextViewLineContainingBufferPosition(anchor).GetCharacterBounds(anchor).Left);

            // Draw from below the header line (clipped to the view when scrolled out) down
            // through the block, in segments that skip lines with text at the guide column —
            // the block's own braces terminate the segments this way.
            var guideTopLine = lines.GetTextViewLineContainingBufferPosition(guideStart);
            var guideBottomLine = lines.GetTextViewLineContainingBufferPosition(guideEnd);
            var yTop = guideTopLine is null ? lines.FirstVisibleLine.Top : guideTopLine.Bottom;
            var yBottom = guideBottomLine is null ? lines.LastVisibleLine.Bottom : guideBottomLine.Bottom;

            var segmentTop = yTop;
            foreach (var line in lines.GetTextViewLinesIntersectingSpan(new SnapshotSpan(guideStart, guideEnd)))
            {
                if (line.GetBufferPositionFromXCoordinate(x, textOnly: true) is { } intersecting && !char.IsWhiteSpace(intersecting.GetChar()))
                {
                    AddSegment(x, segmentTop, line.Top, brush, drawn);
                    segmentTop = line.Bottom + ContinuationPadding;
                }
            }

            AddSegment(x, segmentTop, yBottom, brush, drawn);
        }
    }

    // An explicit anchor point is used as-is; otherwise the guide aligns with the first
    // non-whitespace character of the header's start line — headers whose hint begins
    // mid-line (e.g. "new" of an initializer with the brace on the next line) must not
    // pull the guide to the middle of the line.
    private int? GetAnchorPoint(IStructureTag tag, Span? headerSpan)
        => tag.GuideLineHorizontalAnchorPoint is { } anchor
            ? Translate(new Span(anchor, 0), tag.Snapshot)?.Start
            : headerSpan is { } header
                ? FirstNonWhitespace(new SnapshotPoint(_view.TextSnapshot, header.Start).GetContainingLine())
                : null;

    private static int FirstNonWhitespace(ITextSnapshotLine line)
    {
        int position = line.Start, end = line.End;
        while (position < end && char.IsWhiteSpace(line.Snapshot[position]))
        {
            position++;
        }

        return position;
    }

    private Span? Translate(Span? span, ITextSnapshot tagSnapshot)
        => span is { } value
            ? new SnapshotSpan(tagSnapshot, value).TranslateTo(_view.TextSnapshot, SpanTrackingMode.EdgeExclusive).Span
            : null;

    private static Span? InferGuideSpan(Span? headerSpan, Span? outliningSpan)
        => headerSpan is { } header && outliningSpan is { } outlining && outlining.End >= header.Start
            ? Span.FromBounds(header.Start, outlining.End)
            : null;

    private void AddSegment(double x, double top, double bottom, IBrush brush, HashSet<(double, double, double)> drawn)
    {
        if (bottom - top < MinLineHeight || !drawn.Add((x, top, bottom)))
        {
            return;
        }

        var guide = new Avalonia.Controls.Shapes.Line
        {
            StartPoint = new Avalonia.Point(x, top),
            EndPoint = new Avalonia.Point(x, bottom),
            Stroke = brush,
            StrokeThickness = 1.0,
        };

        // The geometry is in text-rendering coordinates; owner-controlled placement with an
        // explicit viewport offset (guides are rebuilt on every change).
        if (_layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, tag: null, guide, removedCallback: null))
        {
            Avalonia.Controls.Canvas.SetLeft(guide, -_view.ViewportLeft);
            Avalonia.Controls.Canvas.SetTop(guide, -_view.ViewportTop);
        }
    }

    private IBrush GetBrush()
        => _formatMap.GetProperties(BlockStructureFormatNames.Name)
            .TryGetValue(BlockStructureFormatNames.Foreground, out var value) && value is IBrush brush
            ? brush
            : s_fallbackBrush;
}
