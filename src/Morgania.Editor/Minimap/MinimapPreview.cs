#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

/// <summary>
/// One line of the preview: its number as the editor shows it (0 for none) and its text in runs of one brush each (null
/// for the preview's text colour).
/// </summary>
internal sealed record PreviewLine(int Number, IReadOnlyList<(string Text, IBrush? Brush)> Runs);

/// <summary>
/// How the preview sets its lines: type, colours, and where the line numbers end and the text starts, both measured from
/// the preview's outer left edge (<see cref="NumbersRight"/> 0 for no numbers).
/// </summary>
internal sealed record PreviewStyle(
    Typeface Typeface,
    double FontSize,
    double LineHeight,
    IBrush Foreground,
    IBrush NumberForeground,
    IBrush Background,
    IBrush Border,
    IBrush Highlight,
    double NumbersRight,
    double TextLeft);

/// <summary>
/// The code under the pointer, shown over the editor page beside the minimap on the window's overlay layer, as the
/// editor's other popups are: as wide as the page, its line numbers and text where the editor's own stand. It takes no
/// input: the pointer stays on the minimap, which moves and fills it.
/// </summary>
internal sealed class MinimapPreview : IDisposable
{
    /// <summary>The room between the preview and the minimap.</summary>
    internal const double Gap = 4.0;

    private const double ParkedOffscreen = -100000.0;

    private static readonly Thickness s_border = new(1.0);

    private readonly Border _root;
    private readonly StackPanel _lines;
    private OverlayLayer? _overlay;

    public MinimapPreview()
    {
        _lines = new StackPanel { Orientation = Orientation.Vertical };
        _root = new Border
        {
            Child = _lines,
            BorderThickness = s_border,
            CornerRadius = new CornerRadius(3.0),
            Padding = new Thickness(0.0, 4.0),
            ClipToBounds = true,
            IsHitTestVisible = false,
            IsVisible = false,
        };
    }

    public bool IsOpen => _overlay is not null && _root.IsVisible;

    /// <summary>The line the preview centres on; -1 while it is closed.</summary>
    public int CenterLine { get; private set; } = -1;

    /// <summary>The root of the preview, for tests.</summary>
    internal Control Root => _root;

    /// <summary>Fills the preview with <paramref name="lines"/>, the one at <paramref name="highlighted"/> on the style's highlight.</summary>
    public void SetContent(int centerLine, IReadOnlyList<PreviewLine> lines, int highlighted, PreviewStyle style)
    {
        CenterLine = centerLine;
        _root.Background = style.Background;
        _root.BorderBrush = style.Border;
        _lines.Children.Clear();
        for (int i = 0; i < lines.Count; i++)
        {
            var row = new Panel { Height = style.LineHeight, Background = i == highlighted ? style.Highlight : null };
            if (style.NumbersRight > s_border.Left && lines[i].Number > 0)
            {
                var number = Block(style, style.NumberForeground);
                number.Text = lines[i].Number.ToString(CultureInfo.InvariantCulture);
                number.Width = style.NumbersRight - s_border.Left;
                number.TextAlignment = TextAlignment.Right;
                row.Children.Add(number);
            }

            var text = Block(style, style.Foreground);
            text.Margin = new Thickness(Math.Max(0.0, style.TextLeft - s_border.Left), 0.0, 0.0, 0.0);
            foreach (var (content, brush) in lines[i].Runs)
            {
                var run = new Run(content);
                if (brush is not null)
                {
                    run.Foreground = brush;
                }

                text.Inlines!.Add(run);
            }

            row.Children.Add(text);
            _lines.Children.Add(row);
        }
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
        _overlay?.Children.Remove(_root);
        _overlay = null;
    }

    private static TextBlock Block(PreviewStyle style, IBrush foreground) => new()
    {
        FontFamily = style.Typeface.FontFamily,
        FontStyle = style.Typeface.Style,
        FontWeight = style.Typeface.Weight,
        FontSize = style.FontSize,
        LineHeight = style.LineHeight,
        Foreground = foreground,
        TextWrapping = TextWrapping.NoWrap,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
    };
}
