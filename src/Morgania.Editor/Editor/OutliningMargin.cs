#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The editor-format-map key and property name the outlining margin reads its chevron color
/// from. Hosts theme the margin by setting the property (as an <see cref="IBrush"/>) on the
/// map returned by <see cref="IEditorFormatMapService"/>; left unset, a neutral gray is used.
/// </summary>
public static class OutliningMarginFormatNames
{
    /// <summary>The editor format map key.</summary>
    public const string Name = "Outlining Margin";

    /// <summary>The chevrons' stroke brush.</summary>
    public const string Foreground = "Foreground";
}

/// <summary>
/// The outlining (code folding) margin: one chevron per collapsible region, on the line
/// where the region starts — pointing right when the region is collapsed (always shown),
/// down when expanded (shown while the pointer is over the margin, the VS Code gutter
/// behavior, or always, per <see cref="OutliningMarginOptions.ChevronVisibilityId"/>).
/// Clicking collapses the innermost region starting on the line, or expands a collapsed
/// one; the pointer is a hand over a line that has a chevron.
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(PredefinedMarginNames.Outlining)]
[MarginContainer(PredefinedMarginNames.Left)]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Structured)]
[Order(After = PredefinedMarginNames.LineNumber)]
public sealed class OutliningMarginProvider : IWpfTextViewMarginProvider
{
    private readonly IOutliningManagerService _outliningManagerService;
    private readonly IEditorFormatMapService _formatMapService;

    [ImportingConstructor]
    public OutliningMarginProvider(
        IOutliningManagerService outliningManagerService,
        IEditorFormatMapService formatMapService)
    {
        _outliningManagerService = outliningManagerService;
        _formatMapService = formatMapService;
    }

    public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
    {
        ArgumentNullException.ThrowIfNull(wpfTextViewHost);
        return _outliningManagerService.GetOutliningManager(wpfTextViewHost.TextView) is { } manager
            ? new OutliningMargin(
                wpfTextViewHost.TextView,
                manager,
                _formatMapService.GetEditorFormatMap(wpfTextViewHost.TextView))
            : null;
    }

    private sealed class OutliningMargin : Control, IWpfTextViewMargin
    {
        private static readonly IBrush s_fallbackBrush = new SolidColorBrush(Color.FromRgb(0x85, 0x85, 0x85));

        private static readonly Cursor s_hand = new(StandardCursorType.Hand);

        private readonly IWpfTextView _view;
        private readonly IOutliningManager _manager;
        private readonly IEditorFormatMap _formatMap;
        private bool _pointerOver;
        private bool _isDisposed;

        // The cursor the margin had before the hand replaced it over a chevron, which may be one a host set.
        private Cursor? _restingCursor;
        private bool _showsHand;

        public OutliningMargin(IWpfTextView view, IOutliningManager manager, IEditorFormatMap formatMap)
        {
            _view = view;
            _manager = manager;
            _formatMap = formatMap;
            view.LayoutChanged += (_, _) => Refresh();
            view.ZoomLevelChanged += (_, _) => Refresh();
            view.Options.OptionChanged += (_, _) => Refresh();
            // Region events can be raised from tagger worker threads.
            manager.RegionsChanged += (_, _) => Dispatcher.UIThread.Post(InvalidateVisual);
            manager.RegionsCollapsed += (_, _) => Dispatcher.UIThread.Post(InvalidateVisual);
            manager.RegionsExpanded += (_, _) => Dispatcher.UIThread.Post(InvalidateVisual);
            formatMap.FormatMappingChanged += (_, _) => InvalidateVisual();
            Refresh();
        }

        public Control VisualElement => this;

        public double MarginSize => Bounds.Width;

        public bool Enabled => _view.Options.GetOptionValue(DefaultTextViewHostOptions.OutliningMarginId);

        public ITextViewMargin? GetTextViewMargin(string marginName)
            => string.Equals(marginName, PredefinedMarginNames.Outlining, StringComparison.OrdinalIgnoreCase) ? this : null;

        protected override Size MeasureOverride(Size availableSize)
            => new(18.0 * (_view.ZoomLevel / 100.0), 0.0);

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            // A transparent fill keeps the whole strip hit-testable for clicks and hover.
            context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
            if (_isDisposed || _view.IsClosed || _view.InLayout || !Enabled
                || _view is not ITextView2 view2 || !view2.TryGetTextViewLines(out var textViewLines))
            {
                return;
            }

            var snapshot = _view.TextSnapshot;
            // One chevron per snapshot line: collapsed wins over expanded when several
            // regions start on the same line.
            var states = new Dictionary<int, bool>();
            foreach (var region in _manager.GetAllRegions(textViewLines.FormattedSpan))
            {
                int lineNumber = snapshot.GetLineNumberFromPosition(region.Extent.GetStartPoint(snapshot));
                states[lineNumber] = region.IsCollapsed || (states.TryGetValue(lineNumber, out bool collapsed) && collapsed);
            }

            if (states.Count == 0)
            {
                return;
            }

            double zoom = _view.ZoomLevel / 100.0;
            var pen = new Pen(GetBrush(), 1.2 * zoom, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
            bool showsExpanded = _pointerOver
                || _view.Options.GetOptionValue(OutliningMarginOptions.ChevronVisibilityId) == OutliningChevronVisibility.Always;
            foreach (var line in textViewLines)
            {
                if (!line.IsFirstTextViewLineForSnapshotLine
                    || !states.TryGetValue(line.Start.GetContainingLine().LineNumber, out bool isCollapsed)
                    || (!isCollapsed && !showsExpanded))
                {
                    continue;
                }

                var center = new Point(
                    Bounds.Width / 2.0,
                    (line.TextTop + (line.TextHeight / 2.0) - _view.ViewportTop) * zoom);
                DrawChevron(context, pen, center, 3.5 * zoom, isCollapsed);
            }
        }

        private static void DrawChevron(DrawingContext context, Pen pen, Point center, double size, bool isCollapsed)
        {
            // Collapsed points right, expanded points down.
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                if (isCollapsed)
                {
                    geometryContext.BeginFigure(center + new Point(-size / 2.0, -size), isFilled: false);
                    geometryContext.LineTo(center + new Point(size / 2.0, 0.0));
                    geometryContext.LineTo(center + new Point(-size / 2.0, size));
                }
                else
                {
                    geometryContext.BeginFigure(center + new Point(-size, -size / 2.0), isFilled: false);
                    geometryContext.LineTo(center + new Point(0.0, size / 2.0));
                    geometryContext.LineTo(center + new Point(size, -size / 2.0));
                }

                geometryContext.EndFigure(isClosed: false);
            }

            context.DrawGeometry(brush: null, pen, geometry);
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            _pointerOver = true;
            InvalidateVisual();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            _pointerOver = false;
            ShowHand(false);
            InvalidateVisual();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            ShowHand(LineAt(e.GetPosition(this).Y) is { } line && StartsRegion(line.Start.GetContainingLine()));
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (LineAt(e.GetPosition(this).Y) is { } line)
            {
                ToggleRegionsOnLine(line.Start.GetContainingLine());
                e.Handled = true;
            }
        }

        /// <summary>The text line beside <paramref name="y"/>, in the margin's own coordinates; null outside the text.</summary>
        private ITextViewLine? LineAt(double y)
        {
            if (_isDisposed || _view.IsClosed || _view is not ITextView2 view2 || !view2.TryGetTextViewLines(out var textViewLines))
            {
                return null;
            }

            return textViewLines.GetTextViewLineContainingYCoordinate((y / (_view.ZoomLevel / 100.0)) + _view.ViewportTop);
        }

        /// <summary>Whether a region starts on the line, which is what gives it a chevron.</summary>
        private bool StartsRegion(ITextSnapshotLine snapshotLine)
        {
            var snapshot = snapshotLine.Snapshot;
            foreach (var region in _manager.GetAllRegions(snapshotLine.ExtentIncludingLineBreak))
            {
                var start = region.Extent.GetStartPoint(snapshot);
                if (start >= snapshotLine.Start && start <= snapshotLine.End)
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowHand(bool hand)
        {
            if (hand == _showsHand)
            {
                return;
            }

            if (hand)
            {
                _restingCursor = Cursor;
                Cursor = s_hand;
            }
            else
            {
                Cursor = _restingCursor;
            }

            _showsHand = hand;
        }

        private void ToggleRegionsOnLine(ITextSnapshotLine snapshotLine)
        {
            var snapshot = snapshotLine.Snapshot;
            ICollapsible? innermost = null;
            foreach (var region in _manager.GetAllRegions(snapshotLine.ExtentIncludingLineBreak))
            {
                var start = region.Extent.GetStartPoint(snapshot);
                if (start < snapshotLine.Start || start > snapshotLine.End)
                {
                    continue;
                }

                if (region is ICollapsed collapsed)
                {
                    _manager.Expand(collapsed);
                    return;
                }

                if (innermost is null || start > innermost.Extent.GetStartPoint(snapshot))
                {
                    innermost = region;
                }
            }

            if (innermost is not null)
            {
                _manager.TryCollapse(innermost);
            }
        }

        private void Refresh()
        {
            IsVisible = Enabled;
            InvalidateMeasure();
            InvalidateVisual();
        }

        private IBrush GetBrush()
            => _formatMap.GetProperties(OutliningMarginFormatNames.Name)
                .TryGetValue(OutliningMarginFormatNames.Foreground, out var value) && value is IBrush brush
                ? brush
                : s_fallbackBrush;

        public void Dispose() => _isDisposed = true;
    }
}
