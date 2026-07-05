using System.Composition;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Draws <see cref="ITextMarkerTag"/>s (brace matching, reference highlighting, …) into the
/// view's predefined TextMarker adornment layer, which renders below the text. The VS
/// implementation of this manager lives in the closed-source editor, so the host provides it:
/// a text-marker tag aggregator over the view plus a full redraw on layout and tag changes.
/// Colors come from the editor format map entry named by the tag's <see cref="ITextMarkerTag.Type"/>,
/// per the <see cref="MarkerFormatDefinition"/> contract (fill brush + optional border pen);
/// tags whose type has no format entry draw nothing, like in VS.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[Shared]
[ContentType("Roslyn Languages")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class TextMarkerAdornmentManagerProvider : IWpfTextViewCreationListener
{
    private readonly IViewTagAggregatorFactoryService _aggregatorFactory;
    private readonly IEditorFormatMapService _formatMapService;

    [ImportingConstructor]
    public TextMarkerAdornmentManagerProvider(
        IViewTagAggregatorFactoryService aggregatorFactory,
        IEditorFormatMapService formatMapService)
    {
        _aggregatorFactory = aggregatorFactory;
        _formatMapService = formatMapService;
    }

    public void TextViewCreated(IWpfTextView textView) =>
        _ = new TextMarkerAdornmentManager(
            textView,
            _aggregatorFactory.CreateTagAggregator<ITextMarkerTag>(textView),
            _formatMapService.GetEditorFormatMap(textView));
}

internal sealed class TextMarkerAdornmentManager
{
    private readonly IWpfTextView _view;
    private readonly ITagAggregator<ITextMarkerTag> _aggregator;
    private readonly IEditorFormatMap _formatMap;
    private readonly IAdornmentLayer _layer;

    public TextMarkerAdornmentManager(IWpfTextView view, ITagAggregator<ITextMarkerTag> aggregator, IEditorFormatMap formatMap)
    {
        _view = view;
        _aggregator = aggregator;
        _formatMap = formatMap;
        _layer = view.GetAdornmentLayer(PredefinedAdornmentLayers.TextMarker);

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

        foreach (var mappingTag in _aggregator.GetTags(lines.FormattedSpan))
        {
            var (fill, border) = GetFormat(mappingTag.Tag.Type);
            if (fill is null && border is null)
            {
                continue;
            }

            foreach (var span in mappingTag.Span.GetSpans(_view.TextSnapshot))
            {
                if (span.Length == 0 || !lines.IntersectsBufferSpan(span))
                {
                    continue;
                }

                if (lines.GetTextMarkerGeometry(span) is not { } geometry)
                {
                    continue;
                }

                var marker = new Avalonia.Controls.Shapes.Path
                {
                    Data = geometry,
                    Fill = fill,
                    Stroke = border?.Brush,
                    StrokeThickness = border?.Thickness ?? 0.0,
                };

                // The geometry is in text-rendering coordinates; owner-controlled placement
                // with an explicit viewport offset (markers are rebuilt on every change).
                if (_layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, span, tag: null, marker, removedCallback: null))
                {
                    Avalonia.Controls.Canvas.SetLeft(marker, -_view.ViewportLeft);
                    Avalonia.Controls.Canvas.SetTop(marker, -_view.ViewportTop);
                }
            }
        }
    }

    private (IBrush? Fill, Pen? Border) GetFormat(string markerType)
    {
        var properties = _formatMap.GetProperties(markerType);

        var fill = properties.TryGetValue(MarkerFormatDefinition.FillId, out var fillValue) ? fillValue as IBrush : null;
        // ClassificationFormatDefinition-shaped entries (e.g. Roslyn's WPF "brace matching"
        // definition) carry background keys instead of a marker fill.
        fill ??= properties.TryGetValue(EditorFormatDefinition.BackgroundBrushId, out var background) ? background as IBrush : null;
        if (fill is null && properties.TryGetValue(EditorFormatDefinition.BackgroundColorId, out var colorValue) && colorValue is Color color)
        {
            fill = new SolidColorBrush(color);
        }

        var border = properties.TryGetValue(MarkerFormatDefinition.BorderId, out var borderValue) ? borderValue as Pen : null;
        return (fill, border);
    }
}
