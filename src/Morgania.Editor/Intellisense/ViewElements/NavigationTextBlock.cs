#nullable enable

namespace Microsoft.VisualStudio.Text.Adornments.Implementation;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

/// <summary>
/// A text block that acts as a clickable link when embedded in an
/// <see cref="Avalonia.Controls.Documents.InlineUIContainer"/>: Avalonia inlines are not
/// input elements, so navigable runs are hosted as controls instead. Reports its text
/// baseline through <see cref="TextBlock.BaselineOffsetProperty"/> so the host line aligns
/// it exactly like a run, shows a hand cursor, and invokes <see cref="NavigationAction"/>
/// on click.
/// </summary>
public sealed class NavigationTextBlock : TextBlock
{
    public NavigationTextBlock()
    {
        Cursor = new Cursor(StandardCursorType.Hand);
        Background = Brushes.Transparent;
        Tapped += (_, _) => NavigationAction?.Invoke();
    }

    public Action? NavigationAction { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        // EmbeddedControlRun aligns the control's BaselineOffset with the line's text
        // baseline; left at the default 0, the control's bottom edge would sit on it.
        if (TextLayout is { TextLines.Count: > 0 } layout)
        {
            SetValue(BaselineOffsetProperty, layout.TextLines[0].Baseline + Padding.Top);
        }

        return size;
    }
}
