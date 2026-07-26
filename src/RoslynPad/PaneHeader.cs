using Avalonia;

namespace RoslynPad;

/// <summary>
/// Attaches per-dockable header content to a dock model object (e.g. a pane's tab).
/// The <c>DocumentControl</c> template in <c>DockTheme.axaml</c> presents the active
/// dockable's content at the right end of the tab pill row.
/// </summary>
public sealed class PaneHeader : AvaloniaObject
{
    public static readonly AttachedProperty<object?> ContentProperty =
        AvaloniaProperty.RegisterAttached<PaneHeader, AvaloniaObject, object?>("Content");

    public static object? GetContent(AvaloniaObject obj) => obj.GetValue(ContentProperty);

    public static void SetContent(AvaloniaObject obj, object? value) => obj.SetValue(ContentProperty, value);
}
