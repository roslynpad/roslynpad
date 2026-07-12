#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.VisualStudio.Text.Classification;

/// <summary>
/// Renders a blinking caret at the insertion point of every selection of the broker
/// (the primary caret full-strength, secondary carets dimmed).
/// </summary>
internal sealed class CaretLayer : Control
{
    private static readonly IBrush DefaultPrimaryBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
    private static readonly IBrush DefaultSecondaryBrush = new SolidColorBrush(Color.FromArgb(0xA0, 0xE0, 0xE0, 0xE0));
    private static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(530);

    private IBrush _primaryBrush = DefaultPrimaryBrush;
    private IBrush _secondaryBrush = DefaultSecondaryBrush;

    private readonly WpfTextView _view;
    private readonly DispatcherTimer _blinkTimer;
    private bool _blinkOn = true;
    private string? _preeditText;

    public CaretLayer(WpfTextView view)
    {
        _view = view;
        IsHitTestVisible = false;
        ClipToBounds = true;
        _blinkTimer = new DispatcherTimer(BlinkInterval, DispatcherPriority.Background, OnBlink);
    }

    /// <summary>
    /// Re-resolves the caret brushes from the host-themeable format map entries
    /// (<see cref="CaretFormatNames"/>); called at view creation and on format map changes.
    /// A secondary entry left unset derives from the primary color, dimmed.
    /// </summary>
    public void UpdateBrushes(IEditorFormatMap formatMap)
    {
        _primaryBrush = ReadForeground(formatMap, CaretFormatNames.Primary) ?? DefaultPrimaryBrush;
        _secondaryBrush = ReadForeground(formatMap, CaretFormatNames.Secondary)
            ?? (_primaryBrush is ISolidColorBrush { Color: var primary }
                ? new SolidColorBrush(Color.FromArgb(0xA0, primary.R, primary.G, primary.B))
                : DefaultSecondaryBrush);
        InvalidateVisual();
    }

    private static IBrush? ReadForeground(IEditorFormatMap formatMap, string key)
    {
        var properties = formatMap.GetProperties(key);
        if (properties.TryGetValue(EditorFormatDefinition.ForegroundBrushId, out var brushValue) && brushValue is IBrush brush)
        {
            return brush;
        }

        return properties.TryGetValue(EditorFormatDefinition.ForegroundColorId, out var colorValue) && colorValue is Color color
            ? new SolidColorBrush(color)
            : null;
    }

    /// <summary>Called on layout, focus, or caret changes: reset the blink phase to visible.</summary>
    public void OnViewUpdated()
    {
        _blinkOn = true;
        if (_view.HasAggregateFocus && !_view.IsClosed)
        {
            _blinkTimer.Start();
        }
        else
        {
            _blinkTimer.Stop();
        }

        InvalidateVisual();
    }

    private void OnBlink(object? sender, EventArgs e)
    {
        _blinkOn = !_blinkOn;
        InvalidateVisual();
    }

    /// <summary>Sets the IME composition string rendered at the primary caret (null to clear).</summary>
    public void SetPreeditText(string? text)
    {
        if (_preeditText != text)
        {
            _preeditText = text;
            OnViewUpdated();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_view.IsClosed || _view.InLayout || (_view.Caret.IsHidden))
        {
            return;
        }

        // Render is strictly read-only: never create the broker or trigger a layout here
        // (invalidating a visual during the render pass throws in Avalonia). The view
        // subscribes this layer to broker changes when the broker is created.
        var broker = _view.ExistingBroker;
        if (broker is null)
        {
            return;
        }

        // Unfocused views show a solid (non-blinking) caret; focused views blink.
        if (_view.HasAggregateFocus && !_blinkOn)
        {
            return;
        }

        if (!_view.TryGetTextViewLines(out var textViewLines))
        {
            return;
        }

        foreach (var selection in broker.AllSelections)
        {
            var insertion = selection.InsertionPoint;
            if (!textViewLines.ContainsBufferPosition(insertion.Position))
            {
                continue;
            }

            var line = textViewLines.GetTextViewLineContainingBufferPosition(insertion.Position);
            var bounds = line.GetCharacterBounds(insertion);
            bool isPrimary = selection == broker.PrimarySelection;
            double caretX = bounds.Left - _view.ViewportLeft;
            double preeditWidth = 0.0;

            if (isPrimary && _preeditText is { } preedit)
            {
                // IME composition: provisional text drawn at the caret with an underline;
                // it lives only in this layer, never in the buffer.
                var properties = _view.FormattedLineSource.DefaultTextProperties;
                var formatted = new FormattedText(
                    preedit,
                    properties.CultureInfo ?? System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    properties.Typeface,
                    properties.FontRenderingEmSize,
                    properties.ForegroundBrush);
                var origin = new Point(caretX, line.TextTop - _view.ViewportTop);
                context.DrawText(formatted, origin);
                preeditWidth = formatted.WidthIncludingTrailingWhitespace;
                var underlineY = line.TextBottom - _view.ViewportTop - 1.0;
                context.DrawLine(
                    new Pen(properties.ForegroundBrush, 1.0),
                    new Point(caretX, underlineY),
                    new Point(caretX + preeditWidth, underlineY));
            }

            var rect = new Rect(
                caretX + preeditWidth,
                line.TextTop - _view.ViewportTop,
                isPrimary ? 2.0 : 1.5,
                line.TextHeight);
            context.FillRectangle(isPrimary ? _primaryBrush : _secondaryBrush, rect);
        }
    }
}
