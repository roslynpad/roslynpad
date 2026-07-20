using System.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using RoslynGoToImplementationCommandArgs = Microsoft.CodeAnalysis.Editor.Commanding.Commands.GoToImplementationCommandArgs;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// The editor's right-click menu: navigation, clipboard, and formatting commands dispatched
/// through the same Modern Commanding chain as <see cref="CommandingKeyBridge"/>. Right-clicking
/// outside the selection first moves the caret to the click point (VS behavior), so
/// "Go to Definition" targets the symbol under the pointer. Editing commands disable in
/// read-only views.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("Roslyn Languages")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
[method: ImportingConstructor]
internal sealed class EditorContextMenu(
    IEditorCommandHandlerServiceFactory commandServiceFactory,
    IEditorOperationsFactoryService operationsFactory) : IWpfTextViewCreationListener
{
    public void TextViewCreated(IWpfTextView textView)
    {
        var hotkeys = Application.Current?.PlatformSettings?.HotkeyConfiguration
            ?? throw new InvalidOperationException("The Avalonia platform is not initialized.");
        var meta = hotkeys.CommandModifiers;

        var commanding = commandServiceFactory.GetService(textView);
        var operations = operationsFactory.GetEditorOperations(textView);

        var rename = Item("Rename", Gesture(KeyModifiers.None, Key.F2),
            () => commanding.Execute(static (v, b) => new RenameCommandArgs(v, b), static () => { }));
        var cut = Item("Cut", Gesture(meta, Key.X),
            () => commanding.Execute(static (v, b) => new CutCommandArgs(v, b), () => operations.CutSelection()));
        var paste = Item("Paste", Gesture(meta, Key.V),
            () => commanding.Execute(static (v, b) => new PasteCommandArgs(v, b), () => operations.Paste()));
        var toggleComment = Item("Toggle Line Comment", Gesture(meta, Key.OemQuestion),
            () => commanding.Execute(static (v, b) => new ToggleLineCommentCommandArgs(v, b), static () => { }));
        var formatDocument = Item("Format Document", Gesture(meta | KeyModifiers.Shift, Key.D),
            () => commanding.Execute(static (v, b) => new FormatDocumentCommandArgs(v, b), static () => { }));
        MenuItem[] editingItems = [rename, cut, paste, toggleComment, formatDocument];

        textView.VisualElement.ContextMenu = new ContextMenu
        {
            ItemsSource = new Control[]
            {
                Item("Go to Definition", Gesture(KeyModifiers.None, Key.F12),
                    () => commanding.Execute(static (v, b) => new GoToDefinitionCommandArgs(v, b), static () => { })),
                Item("Go to Implementation", Gesture(meta, Key.F12),
                    () => commanding.Execute(static (v, b) => new RoslynGoToImplementationCommandArgs(v, b), static () => { })),
                new Separator(),
                rename,
                new Separator(),
                cut,
                Item("Copy", Gesture(meta, Key.C),
                    () => commanding.Execute(static (v, b) => new CopyCommandArgs(v, b), () => operations.CopySelection())),
                paste,
                new Separator(),
                toggleComment,
                formatDocument,
            },
        };

        // Tunnel so the caret moves and enablement updates before the menu opens.
        textView.VisualElement.AddHandler(
            InputElement.ContextRequestedEvent,
            (_, e) =>
            {
                var readOnly = textView.Options.GetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId);
                foreach (var item in editingItems)
                {
                    item.IsEnabled = !readOnly;
                }

                MoveCaretToPointer(textView, e);
            },
            RoutingStrategies.Tunnel);
    }

    private static MenuItem Item(string header, string gesture, Action onClick)
    {
        var gestureText = new TextBlock
        {
            Text = gesture,
            Margin = new Thickness(24, 0, 0, 0),
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
        };
        DockPanel.SetDock(gestureText, Dock.Right);

        var item = new MenuItem
        {
            Header = new DockPanel
            {
                Children =
                {
                    gestureText,
                    new TextBlock { Text = header, VerticalAlignment = VerticalAlignment.Center },
                },
            },
        };
        item.Click += (_, _) => onClick();
        return item;
    }

    /// <summary>
    /// Platform shortcut text: symbol-style without separators on macOS (⇧⌘D), the
    /// conventional +-joined form elsewhere.
    /// </summary>
    private static string Gesture(KeyModifiers modifiers, Key key)
    {
        var keyName = key switch
        {
            Key.OemQuestion or Key.Oem2 => "/",
            _ => key.ToString(),
        };

        if (Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers == KeyModifiers.Meta)
        {
            return string.Concat(
                modifiers.HasFlag(KeyModifiers.Control) ? "⌃" : string.Empty,
                modifiers.HasFlag(KeyModifiers.Alt) ? "⌥" : string.Empty,
                modifiers.HasFlag(KeyModifiers.Shift) ? "⇧" : string.Empty,
                modifiers.HasFlag(KeyModifiers.Meta) ? "⌘" : string.Empty,
                keyName);
        }

        var parts = new List<string>(4);
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        parts.Add(keyName);
        return string.Join("+", parts);
    }

    private static void MoveCaretToPointer(IWpfTextView view, ContextRequestedEventArgs e)
    {
        // Keyboard-invoked (menu key): keep the caret where it is.
        if (view.IsClosed || !e.TryGetPosition(view.VisualElement, out var point) ||
            view.TextViewLines is not { Count: > 0 } lines)
        {
            return;
        }

        var line = lines.GetTextViewLineContainingYCoordinate(point.Y + view.ViewportTop)
            ?? (point.Y + view.ViewportTop < lines.FirstVisibleLine.Top ? lines.FirstVisibleLine : lines.LastVisibleLine);
        var position = line.GetBufferPositionFromXCoordinate(point.X + view.ViewportLeft) ?? line.End;

        if (view.Selection.IsEmpty || !view.Selection.StreamSelectionSpan.SnapshotSpan.Contains(position))
        {
            view.Selection.Clear();
            view.Caret.MoveTo(position);
        }
    }
}
