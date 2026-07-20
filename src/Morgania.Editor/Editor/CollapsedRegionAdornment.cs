#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

using IViewElementFactoryService = Microsoft.VisualStudio.Text.Adornments.IViewElementFactoryService;
using ToolTipParameters = Microsoft.VisualStudio.Text.Adornments.ToolTipParameters;
using ToolTipPresenter = Microsoft.VisualStudio.Text.Adornments.Implementation.ToolTipPresenter;

/// <summary>
/// The editor-format-map key and property name the collapsed-region adornment reads its
/// color from. Hosts theme the pill by setting the property (as an <see cref="IBrush"/>)
/// on the map returned by <see cref="IEditorFormatMapService"/>; left unset, a neutral
/// gray is used.
/// </summary>
public static class CollapsedAdornmentFormatNames
{
    /// <summary>The editor format map key.</summary>
    public const string Name = "Collapsed Text Adornment";

    /// <summary>The pill's text and border brush.</summary>
    public const string Foreground = "Foreground";
}

/// <summary>
/// Renders each collapsed outlining region as its tag's collapsed form (typically "…") in a
/// clickable pill: an intra-text adornment over the region's last character — the one the
/// elision keeps visible (<see cref="OutliningElisionSupport"/>) — so the pill negotiates
/// its space inline, including when visible text follows the collapse on the same line.
/// Clicking expands the region; resting the pointer on the pill shows the collapsed hint
/// form through the Modern ToolTip presenter (so a classified hint renders colorized),
/// on the editor's own background ("TextView Background") rather than the popup gray —
/// the hint is a preview of editor content, the way the VS hint hosts a mini editor view.
/// Text hover does not fire over the pill (adornment-replaced spans are not text), so the
/// hint is the only popup there.
/// </summary>
[Export(typeof(IViewTaggerProvider))]
[ContentType("any")]
[TextViewRole(PredefinedTextViewRoles.Structured)]
[TagType(typeof(XPlatIntraTextAdornmentTag))]
public sealed class CollapsedRegionAdornmentProvider : IViewTaggerProvider
{
    private readonly IOutliningManagerService _outliningManagerService;
    private readonly IEditorFormatMapService _formatMapService;
    private readonly IViewElementFactoryService _viewElementFactory;

    [ImportingConstructor]
    public CollapsedRegionAdornmentProvider(
        IOutliningManagerService outliningManagerService,
        IEditorFormatMapService formatMapService,
        IViewElementFactoryService viewElementFactory)
    {
        _outliningManagerService = outliningManagerService;
        _formatMapService = formatMapService;
        _viewElementFactory = viewElementFactory;
    }

    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        where T : ITag
    {
        ArgumentNullException.ThrowIfNull(textView);
        ArgumentNullException.ThrowIfNull(buffer);
        if (textView is not IWpfTextView view
            || buffer != textView.TextBuffer
            || _outliningManagerService.GetOutliningManager(textView) is not { } manager)
        {
            return null;
        }

        return textView.Properties.GetOrCreateSingletonProperty(
            () => new CollapsedRegionTagger(view, manager, _formatMapService.GetEditorFormatMap(textView), _viewElementFactory)) as ITagger<T>;
    }

    internal sealed class CollapsedRegionTagger : ITagger<XPlatIntraTextAdornmentTag>
    {
        private static readonly IBrush s_fallbackBrush = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));

        private const int HintDelayMilliseconds = 300;

        private readonly IWpfTextView _view;
        private readonly IOutliningManager _manager;
        private readonly IEditorFormatMap _formatMap;
        private readonly IViewElementFactoryService _viewElementFactory;
        private readonly Dictionary<ICollapsed, XPlatIntraTextAdornmentTag> _tags = [];
        private Avalonia.Threading.DispatcherTimer? _hintTimer;
        private ToolTipPresenter? _hintPresenter;

        public CollapsedRegionTagger(IWpfTextView view, IOutliningManager manager, IEditorFormatMap formatMap, IViewElementFactoryService viewElementFactory)
        {
            _view = view;
            _manager = manager;
            _formatMap = formatMap;
            _viewElementFactory = viewElementFactory;
            manager.RegionsCollapsed += (_, e) => RaiseTagsChanged(e.CollapsedRegions);
            manager.RegionsExpanded += (_, e) =>
            {
                foreach (var dead in _tags.Keys.Where(static region => !region.IsCollapsed).ToList())
                {
                    _tags.Remove(dead);
                }

                RaiseTagsChanged(e.ExpandedRegions);
            };
            formatMap.FormatMappingChanged += (_, _) => Restyle();
        }

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public IEnumerable<ITagSpan<XPlatIntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            var snapshot = spans[0].Snapshot;
            foreach (var collapsed in _manager.GetCollapsedRegions(spans, exposedRegionsOnly: true))
            {
                var extent = collapsed.Extent.GetSpan(snapshot);
                if (extent.IsEmpty)
                {
                    continue;
                }

                // The pill sits on the region's last character, the one the elision keeps.
                var anchor = new SnapshotSpan(snapshot, extent.End - 1, 1);
                if (spans.IntersectsWith(anchor))
                {
                    yield return new TagSpan<XPlatIntraTextAdornmentTag>(anchor, GetOrCreateTag(collapsed));
                }
            }
        }

        private XPlatIntraTextAdornmentTag GetOrCreateTag(ICollapsed collapsed)
        {
            if (!_tags.TryGetValue(collapsed, out var tag))
            {
                var pill = CreatePill(collapsed);
                pill.Measure(Size.Infinity);
                double height = pill.DesiredSize.Height;
                // A baseline that centers the pill on the line's text (the positioner puts
                // the adornment's baseline on the line's).
                double? baseline = _view.FormattedLineSource is { } source
                    ? source.TextHeightAboveBaseline - ((source.LineHeight - height) / 2.0)
                    : null;
                tag = new XPlatIntraTextAdornmentTag(
                    pill, removalCallback: null, topSpace: null, baseline, textHeight: height, bottomSpace: null, affinity: null);
                _tags[collapsed] = tag;
            }

            return tag;
        }

        private Border CreatePill(ICollapsed collapsed)
        {
            var properties = _view.FormattedLineSource?.DefaultTextProperties;
            var brush = GetBrush();
            var text = new TextBlock
            {
                Text = collapsed.Tag.CollapsedForm?.ToString() ?? "...",
                Foreground = brush,
                FontFamily = properties?.Typeface.FontFamily ?? FontFamily.Default,
                FontSize = (properties?.FontRenderingEmSize ?? 12.0) * 0.85,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            var pill = new Border
            {
                Child = text,
                BorderBrush = brush,
                BorderThickness = new Thickness(1.0),
                CornerRadius = new CornerRadius(3.0),
                Padding = new Thickness(3.0, 0.0),
                Margin = new Thickness(2.0, 0.0),
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Hand),
            };

            pill.PointerEntered += (_, _) => ScheduleHint(collapsed);
            pill.PointerExited += (_, _) => _hintTimer?.Stop();
            pill.PointerPressed += (_, e) =>
            {
                if (!_view.IsClosed && collapsed.IsCollapsed)
                {
                    _hintTimer?.Stop();
                    _hintPresenter?.Dismiss();
                    _manager.Expand(collapsed);
                    e.Handled = true;
                }
            };

            return pill;
        }

        /// <summary>Shows the collapsed hint once the pointer rests on the pill; the
        /// mouse-tracking presenter dismisses itself when the pointer leaves both the
        /// pill's span and the tip.</summary>
        private void ScheduleHint(ICollapsed collapsed)
        {
            _hintTimer?.Stop();
            var timer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(HintDelayMilliseconds),
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                ShowHint(collapsed);
            };
            _hintTimer = timer;
            timer.Start();
        }

        private void ShowHint(ICollapsed collapsed)
        {
            if (_view.IsClosed || !collapsed.IsCollapsed || collapsed.Tag.CollapsedHintForm is not { } hint)
            {
                return;
            }

            var snapshot = _view.TextSnapshot;
            var extent = collapsed.Extent.GetSpan(snapshot);
            if (extent.IsEmpty)
            {
                return;
            }

            _hintPresenter?.Dismiss();
            // The default presenter directly (not through IToolTipService), with the popup
            // background swapped for the editor's: the hint previews editor content. The
            // width cap is lifted (viewport-bounded only) — the hint hosts a text view
            // sized to its code.
            var brushes = Microsoft.VisualStudio.Language.Intellisense.PopupBrushes.Read(_formatMap);
            var presenter = new ToolTipPresenter(
                _view,
                new ToolTipParameters(trackMouse: true),
                _viewElementFactory,
                brushes with { Background = GetEditorBackground(brushes.Background) },
                maxTipWidth: double.PositiveInfinity);
            presenter.Dismissed += (_, _) => _hintPresenter = _hintPresenter == presenter ? null : _hintPresenter;
            _hintPresenter = presenter;
            presenter.StartOrUpdate(
                snapshot.CreateTrackingSpan(new Span(extent.End - 1, 1), SpanTrackingMode.EdgeExclusive),
                [hint]);
        }

        /// <summary>The editor background from the standard "TextView Background"
        /// Fonts-and-Colors entry (the same one hosts theme for the view itself).</summary>
        private IBrush GetEditorBackground(IBrush fallback)
        {
            var properties = _formatMap.GetProperties("TextView Background");
            if (properties.TryGetValue(EditorFormatDefinition.BackgroundColorId, out var color) && color is Color background)
            {
                return new SolidColorBrush(background);
            }

            return properties.TryGetValue(EditorFormatDefinition.BackgroundBrushId, out var value) && value is IBrush brush
                ? brush
                : fallback;
        }

        private void RaiseTagsChanged(IEnumerable<ICollapsible> regions)
        {
            var snapshot = _view.TextBuffer.CurrentSnapshot;
            foreach (var region in regions)
            {
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(region.Extent.GetSpan(snapshot)));
            }
        }

        private void Restyle()
        {
            var brush = GetBrush();
            foreach (var tag in _tags.Values)
            {
                if (tag.Adornment is Border { Child: TextBlock text } pill)
                {
                    pill.BorderBrush = brush;
                    text.Foreground = brush;
                }
            }
        }

        private IBrush GetBrush()
            => _formatMap.GetProperties(CollapsedAdornmentFormatNames.Name)
                .TryGetValue(CollapsedAdornmentFormatNames.Foreground, out var value) && value is IBrush brush
                ? brush
                : s_fallbackBrush;
    }
}
