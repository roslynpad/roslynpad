#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// How the preview frames its view: the type and colour of its line numbers, its ground, border and highlight, and where
/// the numbers end and the text starts, both measured from the preview's outer left edge (<see cref="NumbersRight"/> 0
/// for no numbers).
/// </summary>
internal sealed record PreviewStyle(
    Typeface Typeface,
    double FontSize,
    IBrush NumberForeground,
    IBrush Background,
    IBrush Border,
    IBrush Highlight,
    double NumbersRight,
    double TextLeft);

/// <summary>
/// The code under the pointer, shown over the editor page beside the minimap on the window's overlay layer, as the
/// editor's other popups are: a second, read-only text view over the editor's own text, so it is set exactly as the
/// editor sets it, as wide as the page, with line numbers where the editor's own stand. It takes no input: the pointer
/// stays on the minimap, which moves and fills it.
/// </summary>
internal sealed class MinimapPreview : IDisposable
{
    /// <summary>The room between the preview and the minimap.</summary>
    internal const double Gap = 4.0;

    private const double ParkedOffscreen = -100000.0;

    private static readonly Thickness s_border = new(1.0);

    private readonly IWpfTextView _view;
    private readonly Border _root;
    private readonly Canvas _numbers;
    private readonly Border _highlight = new() { IsHitTestVisible = false };
    private OverlayLayer? _overlay;
    private PreviewStyle? _style;
    private int _highlightedLine = -1;

    /// <summary>Frames <paramref name="view"/>, which the preview owns from now on and closes when it is disposed.</summary>
    public MinimapPreview(IWpfTextView view)
    {
        _view = view;
        _numbers = new Canvas { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var text = view.VisualElement;
        text.VerticalAlignment = VerticalAlignment.Top;
        _root = new Border
        {
            Child = new Panel { Children = { _numbers, text } },
            BorderThickness = s_border,
            CornerRadius = new CornerRadius(3.0),
            Padding = new Thickness(0.0, 4.0),
            ClipToBounds = true,
            IsHitTestVisible = false,
            IsVisible = false,
        };
        view.LayoutChanged += OnLayoutChanged;
    }

    public bool IsOpen => _overlay is not null && _root.IsVisible;

    /// <summary>The line the preview centres on, in the editor's visual snapshot; -1 while it is closed.</summary>
    public int CenterLine { get; private set; } = -1;

    /// <summary>The root of the preview, for tests.</summary>
    internal Control Root => _root;

    /// <summary>The view the preview shows, for tests.</summary>
    internal IWpfTextView View => _view;

    /// <summary>The line numbers, one text block per numbered row, for tests.</summary>
    internal Canvas Numbers => _numbers;

    /// <summary>The highlight under the rows of the line the preview centres on, for tests.</summary>
    internal Control Highlight => _highlight;

    /// <summary>
    /// Scrolls the preview so the line of <paramref name="top"/> stands first, <paramref name="rows"/> rows of the view
    /// tall in a preview <paramref name="width"/> wide, the line of <paramref name="highlighted"/> on the style's
    /// highlight; both points are in the view's text snapshot.
    /// </summary>
    public void SetContent(int centerLine, SnapshotPoint top, SnapshotPoint highlighted, int rows, double width, PreviewStyle style)
    {
        CenterLine = centerLine;
        _style = style;
        _highlightedLine = highlighted.GetContainingLine().LineNumber;
        _root.Background = style.Background;
        _root.BorderBrush = style.Border;
        _view.Background = style.Background;
        _highlight.Background = style.Highlight;

        double zoom = _view.ZoomLevel / 100.0;
        double textLeft = Math.Max(0.0, style.TextLeft - s_border.Left);
        double height = rows * _view.LineHeight * zoom;
        var text = _view.VisualElement;
        text.Margin = new Thickness(textLeft, 0.0, 0.0, 0.0);
        text.Height = height;
        _numbers.Width = Math.Max(0.0, style.NumbersRight - s_border.Left);
        _numbers.Height = height;

        // Laid out now at the size the popup gives the view, so the preview opens with its rows, numbers and highlight
        // in place rather than a layout later.
        double viewportWidth = Math.Max(1.0, width - s_border.Left - s_border.Right - textLeft) / zoom;
        _view.DisplayTextLineContainingBufferPosition(top, 0.0, ViewRelativePosition.Top, viewportWidth, height / zoom);
        Decorate();
    }

    /// <summary>
    /// Opens or moves the preview over <paramref name="page"/> (in the anchor's coordinates): as wide as it, centred on
    /// <paramref name="centerY"/> and kept inside it and the window. Stays closed when the anchor has no overlay layer.
    /// </summary>
    public void Place(Visual anchor, Rect page, double centerY)
    {
        if (OverlayLayer.GetOverlayLayer(anchor) is not { } overlay)
        {
            Hide();
            return;
        }

        // Attached before it is measured, so its text has its styles (see the popup agent).
        if (!ReferenceEquals(overlay, _overlay))
        {
            _overlay?.Children.Remove(_root);
            _overlay = overlay;
            Canvas.SetLeft(_root, ParkedOffscreen);
            Canvas.SetTop(_root, ParkedOffscreen);
            overlay.Children.Add(_root);
        }

        if (anchor.TranslatePoint(page.TopLeft, overlay) is not { } topLeft
            || anchor.TranslatePoint(new Point(page.Left, centerY), overlay) is not { } center)
        {
            Hide();
            return;
        }

        _root.Width = Math.Max(0.0, page.Width);
        _root.IsVisible = true;
        _root.Measure(Size.Infinity);
        double height = _root.DesiredSize.Height;
        var window = TopLevel.GetTopLevel(anchor) is { } topLevel ? new Rect(topLevel.ClientSize) : new Rect(overlay.Bounds.Size);
        double top = Math.Max(topLeft.Y, window.Top);
        double bottom = Math.Min(topLeft.Y + page.Height, window.Bottom);
        Canvas.SetLeft(_root, topLeft.X);
        Canvas.SetTop(_root, Math.Clamp(center.Y - (height / 2.0), top, Math.Max(top, bottom - height)));
    }

    public void Hide()
    {
        _root.IsVisible = false;
        CenterLine = -1;
    }

    public void Dispose()
    {
        Hide();
        _view.LayoutChanged -= OnLayoutChanged;
        _overlay?.Children.Remove(_root);
        _overlay = null;
        if (!_view.IsClosed)
        {
            _view.Close();
        }
    }

    private void OnLayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) => Decorate();

    /// <summary>Numbers the rows the view shows, where the editor numbers its own, and lays the highlight under the highlighted line.</summary>
    private void Decorate()
    {
        _numbers.Children.Clear();
        if (_style is not { } style || _view.IsClosed)
        {
            return;
        }

        double zoom = _view.ZoomLevel / 100.0;
        double viewportTop = _view.ViewportTop;
        double viewportBottom = _view.ViewportBottom;
        double? highlightTop = null;
        double highlightBottom = 0.0;
        foreach (var line in _view.TextViewLines)
        {
            // A row that shows less than half of itself, as the one layout rounding lets in under the last, is not counted.
            if (Math.Min(line.Bottom, viewportBottom) - Math.Max(line.Top, viewportTop) < line.Height / 2.0)
            {
                continue;
            }

            int number = line.Start.GetContainingLine().LineNumber;
            if (number == _highlightedLine)
            {
                highlightTop ??= line.Top;
                highlightBottom = line.Bottom;
            }

            if (line.IsFirstTextViewLineForSnapshotLine && _numbers.Width > 0.0)
            {
                var block = new TextBlock
                {
                    Text = (number + 1).ToString(CultureInfo.InvariantCulture),
                    FontFamily = style.Typeface.FontFamily,
                    FontStyle = style.Typeface.Style,
                    FontWeight = style.Typeface.Weight,
                    FontSize = style.FontSize,
                    LineHeight = line.TextHeight * zoom,
                    Height = line.TextHeight * zoom,
                    Width = _numbers.Width,
                    Foreground = style.NumberForeground,
                    TextAlignment = TextAlignment.Right,
                    TextWrapping = TextWrapping.NoWrap,
                };
                Canvas.SetTop(block, (line.TextTop - viewportTop) * zoom);
                _numbers.Children.Add(block);
            }
        }

        // Under the text, as the editor's own current line would be; in the view's coordinates, which the zoom scales.
        var layer = _view.GetAdornmentLayer(PredefinedAdornmentLayers.CurrentLineHighlighter);
        if (highlightTop is not { } highlighted)
        {
            layer.RemoveAdornment(_highlight);
            return;
        }

        if (_highlight.Parent is null)
        {
            layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, visualSpan: null, tag: null, _highlight, removedCallback: null);
        }

        _highlight.Width = _view.ViewportWidth;
        _highlight.Height = highlightBottom - highlighted;
        Canvas.SetLeft(_highlight, 0.0);
        Canvas.SetTop(_highlight, highlighted - viewportTop);
    }
}

/// <summary>
/// The editor's view model as the preview's view sees it: the same buffers and the same elision, so the preview shows the
/// text as the editor does. A view disposes its model when it closes, and this one belongs to the editor's view, so
/// disposing the wrapper leaves it alone.
/// </summary>
internal sealed class PreviewTextViewModel(ITextViewModel model) : ITextViewModel
{
    public PropertyCollection Properties { get; } = new();

    public ITextDataModel DataModel => model.DataModel;

    public ITextBuffer DataBuffer => model.DataBuffer;

    public ITextBuffer EditBuffer => model.EditBuffer;

    public ITextBuffer VisualBuffer => model.VisualBuffer;

    public bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity)
        => model.IsPointInVisualBuffer(editBufferPoint, affinity);

    public SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint)
        => model.GetNearestPointInVisualBuffer(editBufferPoint);

    public SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode)
        => model.GetNearestPointInVisualSnapshot(editBufferPoint, targetVisualSnapshot, trackingMode);

    public void Dispose()
    {
    }
}
