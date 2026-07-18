using System.Collections.Immutable;
using System.Composition;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Presents multi-result go-to searches (Go to Implementation, Go to Definition over partial
/// types, …) as a picker menu at the caret of the focused view — the same presentation style
/// as the light bulb. Single results navigate directly. Navigation itself flows through the
/// host's <see cref="ISymbolNavigationService"/>/<see cref="IDocumentNavigationService"/>, so
/// the presenter stays a pure view-layer concern.
/// </summary>
[Shared]
[Export(typeof(IStreamingFindUsagesPresenter))]
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("Roslyn Languages")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class StreamingFindUsagesPresenter : IStreamingFindUsagesPresenter, IWpfTextViewCreationListener
{
    private IWpfTextView? _activeView;
    private ContextMenu? _menu;

    public void TextViewCreated(IWpfTextView textView)
    {
        textView.GotAggregateFocus += (_, _) => _activeView = textView;
        textView.Closed += (_, _) =>
        {
            if (ReferenceEquals(_activeView, textView))
            {
                _activeView = null;
            }
        };
        _activeView ??= textView;
    }

    public (FindUsagesContext context, CancellationToken cancellationToken) StartSearch(string title, StreamingFindUsagesPresenterOptions options) =>
        (new PickerContext(this, title), CancellationToken.None);

    public void ClearAll() => Dispatcher.UIThread.Post(() => _menu?.Close());

    private async Task PresentAsync(string title, ImmutableArray<DefinitionItem> items, CancellationToken cancellationToken)
    {
        if (items.IsDefaultOrEmpty)
        {
            return;
        }

        if (items.Length == 1)
        {
            await NavigateAsync(items[0], cancellationToken).ConfigureAwait(false);
            return;
        }

        var entries = new List<(string Header, DefinitionItem Item)>();
        foreach (var item in items)
        {
            entries.Add((await GetHeaderAsync(item, cancellationToken).ConfigureAwait(false), item));
        }

        await Dispatcher.UIThread.InvokeAsync(() => ShowMenu(title, entries));
    }

    private static async Task<string> GetHeaderAsync(DefinitionItem item, CancellationToken cancellationToken)
    {
        var name = string.Concat(item.DisplayParts.Select(part => part.Text));
        if (item.SourceSpans.Length == 0)
        {
            return name;
        }

        var documentSpan = item.SourceSpans[0];
        var text = await documentSpan.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var line = text.Lines.GetLineFromPosition(documentSpan.SourceSpan.Start).LineNumber + 1;
        return $"{name} — {documentSpan.Document.Name}:{line}";
    }

    private async Task NavigateAsync(DefinitionItem item, CancellationToken cancellationToken)
    {
        if (GetWorkspace(item) is not { } workspace)
        {
            return;
        }

        var location = await item.GetNavigableLocationAsync(workspace, cancellationToken).ConfigureAwait(false);
        if (location is not null)
        {
            await location.NavigateToAsync(new NavigationOptions(PreferProvisionalTab: true, ActivateTab: true), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The workspace a definition resolves against: its own for in-source items; for metadata
    /// items (symbol-key based) the workspace the search originated in — the focused view's.
    /// </summary>
    private Workspace? GetWorkspace(DefinitionItem item) =>
        item.SourceSpans.Length > 0
            ? item.SourceSpans[0].Document.Project.Solution.Workspace
            : _activeView is { } view && Workspace.TryGetWorkspace(view.TextBuffer.AsTextContainer(), out var workspace)
                ? workspace
                : null;

    private void ShowMenu(string title, List<(string Header, DefinitionItem Item)> entries)
    {
        if (_activeView is not { IsClosed: false } view)
        {
            return;
        }

        var items = new List<Control>
        {
            new MenuItem { Header = title, IsEnabled = false },
            new Separator(),
        };
        foreach (var (header, item) in entries)
        {
            var menuItem = new MenuItem { Header = header };
            menuItem.Click += (_, _) => _ = NavigateAsync(item, CancellationToken.None);
            items.Add(menuItem);
        }

        var caret = view.Caret;
        var menu = new ContextMenu
        {
            ItemsSource = items,
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.BottomLeft,
            PlacementGravity = PopupGravity.BottomRight,
            PlacementRect = new Avalonia.Rect(
                caret.Left - view.ViewportLeft, caret.Top - view.ViewportTop, 1.0, caret.Height),
        };
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_menu, menu))
            {
                _menu = null;
            }
        };

        _menu?.Close();
        _menu = menu;
        menu.Open(view.VisualElement);
    }

    private sealed class PickerContext(StreamingFindUsagesPresenter presenter, string title) : FindUsagesContext
    {
        private readonly List<DefinitionItem> _definitions = [];

        public override ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            lock (_definitions)
            {
                _definitions.Add(definition);
            }

            return default;
        }

        public override async ValueTask OnCompletedAsync(CancellationToken cancellationToken)
        {
            ImmutableArray<DefinitionItem> items;
            lock (_definitions)
            {
                items = [.. _definitions];
            }

            await presenter.PresentAsync(title, items, cancellationToken).ConfigureAwait(false);
        }
    }
}
