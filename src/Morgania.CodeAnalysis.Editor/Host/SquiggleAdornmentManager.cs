using System.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Draws squiggly underlines for <see cref="IErrorTag"/>s into the view's predefined Squiggle
/// adornment layer. The VS implementation of this manager lives in the closed-source editor,
/// so the host provides it: an error-tag aggregator over the view plus a full redraw on layout
/// and tag changes (documents in the demo are small).
/// <para>
/// Over <c>text</c>, not over the Roslyn languages alone: an error tag is language-agnostic, and a host that tags
/// its own content type — a configuration file, a log — would otherwise see its tags aggregated, its messages
/// shown on hover, and nothing drawn under them.
/// </para>
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[Shared]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class SquiggleAdornmentManagerProvider : IWpfTextViewCreationListener
{
    private readonly IViewTagAggregatorFactoryService _aggregatorFactory;
    private readonly IEditorFormatMapService _formatMaps;

    [ImportingConstructor]
    public SquiggleAdornmentManagerProvider(IViewTagAggregatorFactoryService aggregatorFactory, IEditorFormatMapService formatMaps)
    {
        _aggregatorFactory = aggregatorFactory;
        _formatMaps = formatMaps;
    }

    public void TextViewCreated(IWpfTextView textView) =>
        _ = new SquiggleAdornmentManager(
            textView,
            _aggregatorFactory.CreateTagAggregator<IErrorTag>(textView),
            _formatMaps.GetEditorFormatMap(textView));
}

internal sealed class SquiggleAdornmentManager
{
    // VS dark-theme squiggle colors, matching the palette in ClassificationFormats. A host that themes an error
    // type through the editor format map is followed instead; these are what is drawn where it themes none.
    private static readonly Dictionary<string, IBrush> s_brushes = new(StringComparer.OrdinalIgnoreCase)
    {
        [PredefinedErrorTypeNames.SyntaxError] = new SolidColorBrush(Color.FromRgb(0xF1, 0x4C, 0x4C)),
        [PredefinedErrorTypeNames.CompilerError] = new SolidColorBrush(Color.FromRgb(0xF1, 0x4C, 0x4C)),
        [PredefinedErrorTypeNames.OtherError] = new SolidColorBrush(Color.FromRgb(0xF1, 0x4C, 0x4C)),
        [PredefinedErrorTypeNames.Warning] = new SolidColorBrush(Color.FromRgb(0xCC, 0xA7, 0x00)),
        [PredefinedErrorTypeNames.Suggestion] = new SolidColorBrush(Color.FromRgb(0x75, 0xBE, 0xFF)),
        [PredefinedErrorTypeNames.HintedSuggestion] = new SolidColorBrush(Color.FromRgb(0xB8, 0xB8, 0xB8)),
    };

    private readonly IWpfTextView _view;
    private readonly ITagAggregator<IErrorTag> _aggregator;
    private readonly IAdornmentLayer _layer;
    private readonly IEditorFormatMap? _formatMap;

    public SquiggleAdornmentManager(IWpfTextView view, ITagAggregator<IErrorTag> aggregator, IEditorFormatMap? formatMap = null)
    {
        _view = view;
        _aggregator = aggregator;
        _formatMap = formatMap;
        _layer = view.GetAdornmentLayer(PredefinedAdornmentLayers.Squiggle);
        if (formatMap is not null)
        {
            formatMap.FormatMappingChanged += (_, _) => Redraw();
        }

        view.LayoutChanged += (_, _) => Redraw();
        // Tag changes can be raised from tagger worker threads.
        aggregator.BatchedTagsChanged += (_, _) => Dispatcher.UIThread.Post(Redraw);
        view.Closed += (_, _) => aggregator.Dispose();
    }

    private void Redraw()
    {
        if (_view.IsClosed || _view.TextViewLines is not { } lines)
        {
            return;
        }

        _layer.RemoveAllAdornments();

        foreach (var mappingTag in _aggregator.GetTags(lines.FormattedSpan))
        {
            foreach (var span in mappingTag.Span.GetSpans(_view.TextSnapshot))
            {
                var visible = span.Intersection(lines.FormattedSpan);
                if (visible is not { Length: > 0 } visibleSpan)
                {
                    continue;
                }

                foreach (var bounds in lines.GetNormalizedTextBounds(visibleSpan))
                {
                    AddSquiggle(bounds.Left, bounds.Right, bounds.TextBottom, mappingTag.Tag.ErrorType);
                }
            }
        }
    }

    private void AddSquiggle(double left, double right, double bottom, string errorType)
    {
        var width = Math.Max(right - left, 4.0);
        var brush = Themed(errorType) ?? s_brushes.GetValueOrDefault(errorType, s_brushes[PredefinedErrorTypeNames.SyntaxError]);

        var squiggle = new Avalonia.Controls.Shapes.Path
        {
            Data = CreateZigzag(width),
            Stroke = brush,
            StrokeThickness = 1.0,
            StrokeJoin = PenLineJoin.Round,
        };

        if (_layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, tag: null, squiggle, removedCallback: null))
        {
            Canvas.SetLeft(squiggle, left - _view.ViewportLeft);
            Canvas.SetTop(squiggle, bottom - 2.0 - _view.ViewportTop);
        }
    }

    /// <summary>
    /// The colour the host gave this error type, null where it gave none: the pen of a marker format, else its
    /// foreground. A host themes its own error types this way rather than by replacing this manager.
    /// </summary>
    private IBrush? Themed(string errorType)
    {
        if (_formatMap?.GetProperties(errorType) is not { } properties)
        {
            return null;
        }

        if (properties.TryGetValue(MarkerFormatDefinition.BorderId, out var border) && border is Pen { Brush: { } pen })
        {
            return pen;
        }

        return properties.TryGetValue(EditorFormatDefinition.ForegroundBrushId, out var foreground) ? foreground as IBrush : null;
    }

    private static StreamGeometry CreateZigzag(double width)
    {
        const double Amplitude = 1.4;
        const double Step = 2.0;

        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(new Point(0.0, Amplitude), isFilled: false);
        var up = true;
        for (var x = Step; x <= width + Step / 2.0; x += Step)
        {
            context.LineTo(new Point(x, up ? 0.0 : Amplitude * 2.0));
            up = !up;
        }

        context.EndFigure(false);
        return geometry;
    }
}
