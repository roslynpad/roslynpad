using System.Collections.Immutable;
using System.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// The host's stand-in for the VS light bulb: gathers actions from the composed
/// <see cref="ISuggestedActionsSourceProvider"/>s (Roslyn's code fixes and refactorings) and
/// presents them as a context menu at the caret, with nested action groups ("Suppress or
/// configure issues", …) as cascading submenus. VS's light bulb broker/presenter were never
/// open-sourced, so the menu is minimal — no previews — but the actions themselves are the
/// real thing: invoking one runs Roslyn's CodeActionEditHandlerService, which applies the
/// workspace change back to the buffer. Also owns the light bulb icon at the caret line
/// (the VS bulb/bulb-with-error/screwdriver imagery, per the best available category).
/// </summary>
[Export]
[Shared]
public sealed class SuggestedActionsControllerFactory
{
    internal ISuggestedActionCategoryRegistryService CategoryRegistry { get; }
    internal IUIThreadOperationExecutor OperationExecutor { get; }
    internal IEnumerable<Lazy<ISuggestedActionsSourceProvider, IDictionary<string, object>>> Providers { get; }

    [ImportingConstructor]
    public SuggestedActionsControllerFactory(
        ISuggestedActionCategoryRegistryService categoryRegistry,
        IUIThreadOperationExecutor operationExecutor,
        [ImportMany] IEnumerable<Lazy<ISuggestedActionsSourceProvider, IDictionary<string, object>>> providers)
    {
        CategoryRegistry = categoryRegistry;
        OperationExecutor = operationExecutor;
        Providers = providers;
    }

    public SuggestedActionsController GetOrCreate(IWpfTextView textView) =>
        textView.Properties.GetOrCreateSingletonProperty(() => new SuggestedActionsController(textView, this));
}

/// <summary>The adornment layer the light bulb margin icon is drawn into.</summary>
internal sealed class SuggestedActionsAdornments
{
    public const string LightBulbLayerName = "SuggestedActionsLightBulb";

    [Export]
    [Name(LightBulbLayerName)]
    [Order(After = PredefinedAdornmentLayers.Caret)]
    public AdornmentLayerDefinition? LightBulbLayer { get; }
}

#pragma warning disable CA1001 // The controller lives as long as its view; the CTS is cancelled on view close.
public sealed class SuggestedActionsController
#pragma warning restore CA1001
{
    private readonly IWpfTextView _view;
    private readonly SuggestedActionsControllerFactory _factory;

    private ImmutableArray<ISuggestedActionsSource>? _sources;
    private List<(ISuggestedAction Action, int Indent)> _entries = [];
    private bool _isComputing;
    private ContextMenu? _menu;

    private const double BulbSize = 16.0;

    private enum BulbPlacement { Detached, View, Margin }

    private IAdornmentLayer? _bulbLayer;
    private Canvas? _marginCanvas;
    private Control? _bulbControl;
    private BulbPlacement _bulbPlacement;
    private string? _bulbCategory;
    private CancellationTokenSource? _bulbCancellation;

    public SuggestedActionsController(IWpfTextView view, SuggestedActionsControllerFactory factory)
    {
        _view = view;
        _factory = factory;

        // The light bulb margin icon tracks the caret like VS's: every caret move or edit
        // hides it and schedules a recomputation of the applicable action categories.
        view.Caret.PositionChanged += (_, _) => ScheduleLightBulbUpdate();
        view.TextBuffer.Changed += (_, _) => ScheduleLightBulbUpdate();
        view.LayoutChanged += (_, _) => PositionLightBulb();
        view.Closed += (_, _) =>
        {
            _bulbCancellation?.Cancel();
            DisposeSources();
        };
        ScheduleLightBulbUpdate();
    }

    public bool IsOpen { get; private set; }

    /// <summary>The actions in the open menu, flattened depth-first (nested groups included).</summary>
    public IReadOnlyList<ISuggestedAction> Actions => [.. _entries.Select(entry => entry.Action)];

    /// <summary>
    /// The category the light bulb icon currently shows —
    /// <see cref="PredefinedSuggestedActionCategoryNames.ErrorFix"/>,
    /// <see cref="PredefinedSuggestedActionCategoryNames.CodeFix"/>,
    /// <see cref="PredefinedSuggestedActionCategoryNames.Refactoring"/>, or null when hidden.
    /// </summary>
    public string? LightBulbCategory => _bulbCategory;

    /// <summary>Fire-and-forget entry point for the keyboard bridge (Ctrl/Cmd+.).</summary>
    public bool Show()
    {
        _ = ShowAsync(CancellationToken.None);
        return true;
    }

    public async Task<bool> ShowAsync(CancellationToken cancellationToken)
    {
        if (_isComputing || _view.IsClosed)
        {
            return false;
        }

        Dismiss();
        _isComputing = true;
        try
        {
            var range = GetActionsRange();

            var entries = new List<(ISuggestedAction, int)>();
            var items = new List<Control>();
            foreach (var source in GetSources())
            {
                var sets = await GetActionSetsAsync(source, range, cancellationToken).ConfigureAwait(true);
                await BuildMenuItemsAsync(sets, entries, items, indent: 0, cancellationToken).ConfigureAwait(true);
            }

            if (entries.Count == 0 || _view.IsClosed)
            {
                return false;
            }

            _entries = entries;
            ShowMenu(items);
            return true;
        }
        finally
        {
            _isComputing = false;
        }
    }

    public bool Invoke(int index)
    {
        if (!IsOpen || index < 0 || index >= _entries.Count)
        {
            return false;
        }

        var action = _entries[index].Action;
        if (action is EditorSuggestedActionWithNestedActions)
        {
            // Group headers are submenu containers, not invokable actions.
            return false;
        }

        Dismiss();

        if (action is EditorSuggestedAction editorAction)
        {
            // ISuggestedAction.Invoke(CancellationToken) throws in modern Roslyn; the
            // light bulb is expected to call the operation-context overload instead.
            using var context = _factory.OperationExecutor.BeginExecute(
                "Suggested Actions", action.DisplayText, allowCancellation: true, showProgress: false);
            editorAction.Invoke(context);
        }
        else
        {
            action.Invoke(CancellationToken.None);
        }

        return true;
    }

    public bool Dismiss()
    {
        _view.Caret.PositionChanged -= OnCaretPositionChanged;
        IsOpen = false;
        if (_menu is { } menu)
        {
            _menu = null;
            menu.Close();
        }

        return true;
    }

    private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e) => Dismiss();

    /// <summary>
    /// The range actions are computed for: the selection when there is one, otherwise the
    /// caret line's extent — the same context the VS light bulb uses, so fixes anywhere on
    /// the caret's line light up.
    /// </summary>
    private SnapshotSpan GetActionsRange()
    {
        if (!_view.Selection.IsEmpty)
        {
            return _view.Selection.StreamSelectionSpan.SnapshotSpan;
        }

        var caretLine = _view.Caret.Position.BufferPosition.GetContainingLine();
        return new SnapshotSpan(caretLine.Start, caretLine.End);
    }

    private async Task BuildMenuItemsAsync(
        IEnumerable<SuggestedActionSet> sets,
        List<(ISuggestedAction, int)> entries,
        List<Control> items,
        int indent,
        CancellationToken cancellationToken)
    {
        foreach (var set in sets)
        {
            var separatorPending = items.Count > 0;
            foreach (var action in set.Actions)
            {
                if (separatorPending)
                {
                    items.Add(new Separator());
                    separatorPending = false;
                }

                var index = entries.Count;
                entries.Add((action, indent));

                var text = action is ISuggestedAction2 { DisplayTextSuffix.Length: > 0 } action2
                    ? $"{action.DisplayText} {action2.DisplayTextSuffix}"
                    : action.DisplayText;
                // DisplayText is already menu-escaped: Roslyn doubles '_' because the VS light
                // bulb is a menu with access keys, and Avalonia MenuItem headers collapse the
                // doubling the same way (this is why a plain-text presenter shows '__').
                var item = new MenuItem { Header = text };

                // Only real nested action groups ("Suppress or configure issues", …) become
                // submenus, not the flavor sets (preview/fix-all links) every ordinary Roslyn
                // action reports.
                if (action is EditorSuggestedActionWithNestedActions && action.HasActionSets &&
                    await action.GetActionSetsAsync(cancellationToken).ConfigureAwait(true) is { } nestedSets)
                {
                    var children = new List<Control>();
                    await BuildMenuItemsAsync(nestedSets, entries, children, indent + 1, cancellationToken).ConfigureAwait(true);
                    item.ItemsSource = children;
                }
                else
                {
                    item.Click += (_, _) => Invoke(index);
                }

                items.Add(item);
            }
        }
    }

    private void ShowMenu(List<Control> items)
    {
        var caret = _view.Caret;
        var menu = new ContextMenu
        {
            ItemsSource = items,
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.BottomLeft,
            PlacementGravity = PopupGravity.BottomRight,
            PlacementRect = new Rect(
                caret.Left - _view.ViewportLeft, caret.Top - _view.ViewportTop, 1.0, caret.Height),
        };
        menu.Closed += (_, _) =>
        {
            // Closed by the menu itself (Escape, click-away, item click).
            if (ReferenceEquals(_menu, menu))
            {
                _menu = null;
                _view.Caret.PositionChanged -= OnCaretPositionChanged;
                IsOpen = false;
            }
        };

        _menu = menu;
        menu.Open(_view.VisualElement);
        IsOpen = true;
        _view.Caret.PositionChanged += OnCaretPositionChanged;
    }

    private ImmutableArray<ISuggestedActionsSource> GetSources()
    {
        if (_sources is { } existing)
        {
            return existing;
        }

        var buffer = _view.TextBuffer;
        var sources = _factory.Providers
            .Where(provider => GetContentTypes(provider.Metadata).Any(buffer.ContentType.IsOfType))
            .Select(provider => provider.Value.CreateSuggestedActionsSource(_view, buffer))
            .Where(source => source is not null)
            .ToImmutableArray();
        _sources = sources;
        return sources;
    }

    private static IEnumerable<string> GetContentTypes(IDictionary<string, object> metadata) =>
        metadata.TryGetValue("ContentTypes", out var value)
            ? value switch
            {
                string single => [single],
                IEnumerable<string> many => many,
                _ => [],
            }
            : [];

    private async Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(
        ISuggestedActionsSource source, SnapshotSpan range, CancellationToken cancellationToken)
    {
        var categories = _factory.CategoryRegistry.AllCodeFixesAndRefactorings;

        if (source is IAsyncSuggestedActionsSource asyncSource)
        {
            // Roslyn asserts the collectors match its exported [SuggestedActionPriority]
            // orderings, highest first; it is the only async source in this composition.
            var collectors = SuggestedActionsSourceProvider.Orderings
                .Select(ordering => new Collector(ordering))
                .ToImmutableArray();
            await asyncSource.GetSuggestedActionsAsync(
                categories, range, [.. collectors.Cast<ISuggestedActionSetCollector>()], cancellationToken).ConfigureAwait(true);
            return collectors.SelectMany(collector => collector.Sets);
        }

        return source.GetSuggestedActions(categories, range, cancellationToken) ?? [];
    }

    private void ScheduleLightBulbUpdate()
    {
        HideLightBulb();
        _bulbCancellation?.Cancel();
        _bulbCancellation = new CancellationTokenSource();
        _ = UpdateLightBulbAsync(_bulbCancellation.Token);
    }

    private async Task UpdateLightBulbAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Debounce caret movement/typing (Roslyn adds its own 100ms on top).
            await Task.Delay(200, cancellationToken).ConfigureAwait(true);
            if (_view.IsClosed)
            {
                return;
            }

            var range = GetActionsRange();
            var categories = _factory.CategoryRegistry.AllCodeFixesAndRefactorings;

            string? best = null;
            foreach (var source in GetSources())
            {
                if (source is not ISuggestedActionsSource2 source2)
                {
                    continue;
                }

                var applicable = await source2.GetSuggestedActionCategoriesAsync(categories, range, cancellationToken).ConfigureAwait(true);
                foreach (var category in applicable ?? Enumerable.Empty<string>())
                {
                    best = best is null || Rank(category) > Rank(best) ? category : best;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (best is null)
            {
                HideLightBulb();
            }
            else
            {
                ShowLightBulb(best);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"light bulb update failed: {ex}");
        }

        static int Rank(string category) => category switch
        {
            PredefinedSuggestedActionCategoryNames.ErrorFix => 3,
            PredefinedSuggestedActionCategoryNames.CodeFix or PredefinedSuggestedActionCategoryNames.StyleFix => 2,
            _ => 1,
        };
    }

    private void ShowLightBulb(string category)
    {
        // The VS light bulb imagery: error fixes get the bulb with the error badge, other
        // fixes the plain bulb, refactorings the screwdriver.
        var (imageName, toolTip) = category switch
        {
            PredefinedSuggestedActionCategoryNames.ErrorFix =>
                ("IntellisenseLightBulbError", "Critical action to fix an error in your code, or apply necessary refactoring."),
            PredefinedSuggestedActionCategoryNames.CodeFix or PredefinedSuggestedActionCategoryNames.StyleFix =>
                ("IntellisenseBulb", "Recommended action to address noncritical issues with your code."),
            _ =>
                ("Screwdriver", "Suggested action to improve your code."),
        };

        HideLightBulb();
        var bulb = new Border
        {
            Width = 16.0,
            Height = 16.0,
            Background = Brushes.Transparent,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Child = new Image
            {
                Width = 16.0,
                Height = 16.0,
                Source = Morgania.CodeAnalysis.Editor.ImageCatalog.GetImage(imageName),
            },
        };
        ToolTip.SetTip(bulb, toolTip);
        bulb.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            Show();
        };

        _bulbControl = bulb;
        _bulbCategory = category;
        PositionLightBulb();
    }

    /// <summary>
    /// VS placement — the bulb never covers code: it goes over the caret line's leading
    /// whitespace when it fits there, else onto an adjacent blank line, else into the
    /// glyph margin.
    /// </summary>
    private void PositionLightBulb()
    {
        if (_bulbControl is null || _view.IsClosed || _view.TextViewLines is not { } lines)
        {
            return;
        }

        var caret = _view.Caret.Position.BufferPosition;
        if (!lines.ContainsBufferPosition(caret))
        {
            HideLightBulb();
            return;
        }

        var line = lines.GetTextViewLineContainingBufferPosition(caret);
        if (GetLeadingWhitespaceRoom(line) >= BulbSize)
        {
            PlaceBulbInView(line, line);
        }
        else if (GetAdjacentBlankLine(lines, line) is { } blankLine)
        {
            PlaceBulbInView(line, blankLine);
        }
        else if (GetMarginCanvas() is { } margin)
        {
            AttachBulb(BulbPlacement.Margin);
            Canvas.SetLeft(_bulbControl, Math.Max(0.0, (margin.Bounds.Width - BulbSize) / 2.0));
            Canvas.SetTop(_bulbControl, GetLineCenterY(line));
        }
        else
        {
            // No glyph margin in this host; the line start is the least intrusive fallback.
            PlaceBulbInView(line, line);
        }
    }

    /// <summary>
    /// The pixels between the line's start and its first non-whitespace character —
    /// unbounded for blank lines, zero when the line starts with code.
    /// </summary>
    private static double GetLeadingWhitespaceRoom(ITextViewLine line)
    {
        var text = line.Extent.GetText();
        int firstNonWhitespace = 0;
        while (firstNonWhitespace < text.Length && char.IsWhiteSpace(text[firstNonWhitespace]))
        {
            firstNonWhitespace++;
        }

        if (firstNonWhitespace == text.Length)
        {
            return double.PositiveInfinity;
        }

        return firstNonWhitespace == 0
            ? 0.0
            : line.GetCharacterBounds(line.Start + firstNonWhitespace).Left - line.TextLeft;
    }

    private static ITextViewLine? GetAdjacentBlankLine(ITextViewLineCollection lines, ITextViewLine line)
    {
        int index = lines.GetIndexOfTextLine(line);
        var above = index > 0 ? lines[index - 1] : null;
        var below = index >= 0 && index < lines.Count - 1 ? lines[index + 1] : null;
        return above is not null && IsBlank(above) ? above
            : below is not null && IsBlank(below) ? below
            : null;

        static bool IsBlank(ITextViewLine line) => string.IsNullOrWhiteSpace(line.Extent.GetText());
    }

    private void PlaceBulbInView(ITextViewLine caretLine, ITextViewLine targetLine)
    {
        AttachBulb(BulbPlacement.View);
        Canvas.SetLeft(_bulbControl!, caretLine.TextLeft - _view.ViewportLeft);
        Canvas.SetTop(_bulbControl!, GetLineCenterY(targetLine));
    }

    private double GetLineCenterY(ITextViewLine line)
        => line.TextTop + (line.TextHeight - BulbSize) / 2.0 - _view.ViewportTop;

    private void AttachBulb(BulbPlacement placement)
    {
        if (_bulbControl is null || _bulbPlacement == placement)
        {
            return;
        }

        DetachBulb();
        if (placement == BulbPlacement.Margin)
        {
            _marginCanvas!.Children.Add(_bulbControl);
        }
        else
        {
            _bulbLayer ??= _view.GetAdornmentLayer(SuggestedActionsAdornments.LightBulbLayerName);
            if (!_bulbLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, tag: null, _bulbControl, removedCallback: null))
            {
                return;
            }
        }

        _bulbPlacement = placement;
    }

    private void DetachBulb()
    {
        if (_bulbControl is not null)
        {
            switch (_bulbPlacement)
            {
                case BulbPlacement.View:
                    _bulbLayer?.RemoveAdornment(_bulbControl);
                    break;
                case BulbPlacement.Margin:
                    _marginCanvas?.Children.Remove(_bulbControl);
                    break;
            }
        }

        _bulbPlacement = BulbPlacement.Detached;
    }

    /// <summary>
    /// The (otherwise empty) glyph margin hosts the bulb when there is no whitespace to
    /// place it over — reached through the view host in the view's property bag.
    /// </summary>
    private Canvas? GetMarginCanvas()
    {
        if (_marginCanvas is null
            && _view.Properties.TryGetProperty(typeof(IWpfTextViewHost), out IWpfTextViewHost? host)
            && host?.GetTextViewMargin(PredefinedMarginNames.Glyph)?.VisualElement is Border border)
        {
            if (border.Child is not Canvas canvas)
            {
                canvas = new Canvas { ClipToBounds = true };
                border.Child = canvas;
            }

            _marginCanvas = canvas;
        }

        return _marginCanvas;
    }

    private void HideLightBulb()
    {
        DetachBulb();
        _bulbControl = null;
        _bulbCategory = null;
    }

    private void DisposeSources()
    {
        Dismiss();
        foreach (var source in _sources ?? [])
        {
            source.Dispose();
        }

        _sources = null;
    }

    private sealed class Collector(string priority) : ISuggestedActionSetCollector
    {
        private readonly List<SuggestedActionSet> _sets = [];

        public string Priority => priority;

        public IEnumerable<SuggestedActionSet> Sets
        {
            get
            {
                lock (_sets)
                {
                    return [.. _sets];
                }
            }
        }

        public void Add(SuggestedActionSet set)
        {
            lock (_sets)
            {
                _sets.Add(set);
            }
        }

        public void Complete()
        {
        }
    }
}
