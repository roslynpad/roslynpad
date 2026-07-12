#nullable enable

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation;

using System.Collections.Immutable;
using System.Composition;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The default async completion presenter provider. One presenter is kept per view (the
/// contract encourages reuse); the async completion broker picks this provider unless an
/// extender orders one before it.
/// </summary>
[Export(typeof(ICompletionPresenterProvider))]
[Name(PredefinedCompletionNames.DefaultCompletionPresenter)]
[ContentType("any")]
public sealed class CompletionPresenterProvider : ICompletionPresenterProvider
{
    private readonly Lazy<IViewElementFactoryService> _viewElementFactory;
    private readonly Lazy<IEditorFormatMapService> _editorFormatMaps;

    [ImportingConstructor]
    public CompletionPresenterProvider(
        Lazy<IViewElementFactoryService> viewElementFactory,
        Lazy<IEditorFormatMapService> editorFormatMaps)
    {
        _viewElementFactory = viewElementFactory;
        _editorFormatMaps = editorFormatMaps;
    }

    public CompletionPresenterOptions Options { get; } = new(resultsPerPage: 9);

    public ICompletionPresenter GetOrCreate(ITextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        return textView.Properties.GetOrCreateSingletonProperty(
            typeof(CompletionPresenter),
            () => new CompletionPresenter(textView, _viewElementFactory.Value, _editorFormatMaps.Value.GetEditorFormatMap(textView)));
    }
}

/// <summary>
/// The completion UI per the async completion walkthrough: the item list with the pattern's
/// highlighted spans, filter toggles, soft vs. full selection, and the suggestion-mode item
/// row. Shown through the "completion" space reservation manager, anchored to the session's
/// applicable span. Keyboard is the session's job (<see cref="IAsyncCompletionSession"/>
/// handles up/down/commit); the presenter renders state and reports mouse gestures.
/// </summary>
internal sealed class CompletionPresenter : ICompletionPresenter
{
    private readonly ITextView _view;
    private readonly IViewElementFactoryService _viewElementFactory;
    private readonly IEditorFormatMap _editorFormatMap;
    private readonly Border _container;
    private readonly StackPanel _itemsPanel = new();
    private readonly ScrollViewer _scroll;
    private readonly StackPanel _filtersPanel = new() { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 2.0 };
    private readonly Border _suggestionRow;
    private readonly Border _filtersHost;
    private readonly TextBlock _suggestionText = new() { FontStyle = FontStyle.Italic };
    private PopupBrushes _brushes;
    private ISpaceReservationManager? _manager;
    private ISpaceReservationAgent? _agent;
    private IAsyncCompletionSession? _session;
    private CancellationTokenSource? _descriptionCancellation;
    private CompletionItem? _describedItem;
    private bool _isOpen;
    private bool _updatingFilters;

    public CompletionPresenter(ITextView view, IViewElementFactoryService viewElementFactory, IEditorFormatMap editorFormatMap)
    {
        _view = view;
        _viewElementFactory = viewElementFactory;
        _editorFormatMap = editorFormatMap;
        _brushes = PopupBrushes.Read(editorFormatMap);
        _scroll = new ScrollViewer
        {
            Content = _itemsPanel,
            MaxHeight = 220.0,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        _suggestionRow = new Border
        {
            Child = _suggestionText,
            Padding = new Thickness(4.0, 2.0),
            BorderThickness = new Thickness(1.0),
            BorderBrush = Brushes.Transparent,
            IsVisible = false,
        };
        var dock = new DockPanel();
        DockPanel.SetDock(_suggestionRow, Dock.Top);
        _filtersHost = new Border
        {
            Child = _filtersPanel,
            Padding = new Thickness(4.0, 3.0),
            BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0),
        };
        DockPanel.SetDock(_filtersHost, Dock.Bottom);
        dock.Children.Add(_suggestionRow);
        dock.Children.Add(_filtersHost);
        dock.Children.Add(_scroll);
        _container = new Border
        {
            Child = dock,
            BorderThickness = new Thickness(1.0),
            MinWidth = 180.0,
            MaxWidth = 480.0,
        };
        ApplyBrushes();
    }

    private void ApplyBrushes()
    {
        _filtersHost.BorderBrush = _brushes.BorderBrush;
        _container.Background = _brushes.Background;
        _container.BorderBrush = _brushes.BorderBrush;
        _container.SetValue(TextElement.ForegroundProperty, _brushes.Foreground);
    }

    public event EventHandler<CompletionFilterChangedEventArgs>? FiltersChanged;

    public event EventHandler<CompletionItemSelectedEventArgs>? CompletionItemSelected;

    public event EventHandler<CompletionItemEventArgs>? CommitRequested;

    public event EventHandler<CompletionClosedEventArgs>? CompletionClosed;

    internal Control SurfaceElement => _container;

    internal IReadOnlyList<CompletionItemWithHighlight> VisibleItems { get; private set; } = [];

    internal int SelectedIndex { get; private set; } = -1;

    internal bool IsSoftSelection { get; private set; }

    internal bool IsSuggestionRowVisible => _suggestionRow.IsVisible;

    public void Open(IAsyncCompletionSession session, CompletionPresentationViewModel presentation)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(presentation);
        _session = session;
        Render(presentation);
        if (_agent is null)
        {
            _manager = _view.GetSpaceReservationManager(IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName);
            _agent = _manager.CreatePopupAgent(presentation.ApplicableToSpan, PopupStyles.None, _container);
            _manager.AgentChanged += OnAgentChanged;
            _manager.AddAgent(_agent);
        }

        _isOpen = true;
        ScheduleDescriptionUpdate();
    }

    public void Update(IAsyncCompletionSession session, CompletionPresentationViewModel presentation)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(presentation);
        _session = session;
        Render(presentation);
        if (_agent is not null)
        {
            _manager!.UpdatePopupAgent(_agent, presentation.ApplicableToSpan, PopupStyles.None);
        }

        ScheduleDescriptionUpdate();
    }

    public void Close()
    {
        _descriptionCancellation?.Cancel();
        _describedItem = null;
        _session = null;
        if (_agent is { } agent)
        {
            // The session already unhooked CompletionClosed when it initiated the close;
            // when the popup host drops the agent instead, OnAgentChanged reports it.
            _manager!.AgentChanged -= OnAgentChanged;
            _agent = null;
            _manager.RemoveAgent(agent);
        }

        _isOpen = false;
    }

    public void Dispose() => Close();

    private void OnAgentChanged(object? sender, SpaceReservationAgentChangedEventArgs e)
    {
        if (_isOpen && ReferenceEquals(e.OldAgent, _agent) && e.NewAgent is null)
        {
            _agent = null;
            _isOpen = false;
            CompletionClosed?.Invoke(this, new CompletionClosedEventArgs(_view));
        }
    }

    private void Render(CompletionPresentationViewModel presentation)
    {
        VisibleItems = presentation.Items;
        SelectedIndex = presentation.SelectSuggestionItem ? -1 : presentation.SelectedItemIndex;
        IsSoftSelection = presentation.UseSoftSelection;

        // The presenter lives as long as its view; the host may have re-themed the popup
        // palette since the last session.
        _brushes = PopupBrushes.Read(_editorFormatMap);
        ApplyBrushes();

        RenderSuggestionRow(presentation);
        RenderItems(presentation);
        RenderFilters(presentation);
    }

    private void RenderSuggestionRow(CompletionPresentationViewModel presentation)
    {
        _suggestionRow.IsVisible = presentation.DisplaySuggestionItem;
        if (!presentation.DisplaySuggestionItem)
        {
            return;
        }

        string text = presentation.SuggestionItem?.DisplayText is { Length: > 0 } display
            ? display
            : presentation.SuggestionItemOptions.DisplayTextWhenEmpty;
        _suggestionText.Text = text;
        ToolTip.SetTip(_suggestionRow, presentation.SuggestionItemOptions.ToolTipText);
        bool selected = presentation.SelectSuggestionItem;
        _suggestionRow.Background = selected && !presentation.UseSoftSelection ? _brushes.SelectionBackground : Brushes.Transparent;
        _suggestionRow.BorderBrush = selected && presentation.UseSoftSelection ? _brushes.SoftSelectionBorder : Brushes.Transparent;
    }

    private void RenderItems(CompletionPresentationViewModel presentation)
    {
        _itemsPanel.Children.Clear();
        bool anyIcon = presentation.Items.Any(static item => item.CompletionItem.Icon is not null);
        for (int i = 0; i < presentation.Items.Length; i++)
        {
            var item = presentation.Items[i];
            bool selected = !presentation.SelectSuggestionItem && i == presentation.SelectedItemIndex;
            bool fullSelection = selected && !presentation.UseSoftSelection;
            var row = new Border
            {
                Padding = new Thickness(6.0, 1.0),
                BorderThickness = new Thickness(1.0),
                // Soft selection marks the selected item without claiming it: an outline
                // instead of the filled selection (the walkthrough's soft-selection visual).
                Background = fullSelection ? _brushes.SelectionBackground : Brushes.Transparent,
                BorderBrush = selected && presentation.UseSoftSelection ? _brushes.SoftSelectionBorder : Brushes.Transparent,
                Child = BuildItemContent(item, anyIcon),
                Tag = item.CompletionItem,
            };
            if (fullSelection)
            {
                row.SetValue(TextElement.ForegroundProperty, _brushes.SelectionForeground);
            }

            row.PointerPressed += OnRowPressed;
            row.DoubleTapped += OnRowDoubleTapped;
            _itemsPanel.Children.Add(row);

            if (selected)
            {
                var selectedRow = row;
                Dispatcher.UIThread.Post(() => selectedRow.BringIntoView(), DispatcherPriority.Loaded);
            }
        }
    }

    private Control BuildItemContent(CompletionItemWithHighlight item, bool anyIcon)
    {
        var block = BuildItemText(item);
        if (!anyIcon)
        {
            return block;
        }

        // A fixed-size icon slot keeps the text column aligned for items without an icon.
        Control icon = item.CompletionItem.Icon is { } imageElement
            && _viewElementFactory.CreateViewElement<Control>(_view, imageElement) is { } element
            ? element
            : new Border { Width = 16.0, Height = 16.0 };
        icon.Margin = new Thickness(0.0, 0.0, 4.0, 0.0);
        icon.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;

        var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        panel.Children.Add(icon);
        panel.Children.Add(block);
        return panel;
    }

    private TextBlock BuildItemText(CompletionItemWithHighlight item)
    {
        var block = new TextBlock();
        string text = item.CompletionItem.DisplayText;
        int consumed = 0;
        foreach (var highlight in item.HighlightedSpans)
        {
            if (highlight.Start > consumed)
            {
                block.Inlines!.Add(new Run(text[consumed..highlight.Start]));
            }

            block.Inlines!.Add(new Run(text.Substring(highlight.Start, highlight.Length))
            {
                FontWeight = FontWeight.Bold,
                Foreground = _brushes.MatchForeground,
            });
            consumed = highlight.End;
        }

        if (consumed < text.Length)
        {
            block.Inlines!.Add(new Run(text[consumed..]));
        }

        if (item.CompletionItem.Suffix is { Length: > 0 } suffix)
        {
            block.Inlines!.Add(new Run("  " + suffix) { Foreground = _brushes.DeemphasizedForeground });
        }

        return block;
    }

    private void RenderFilters(CompletionPresentationViewModel presentation)
    {
        _updatingFilters = true;
        try
        {
            _filtersPanel.Children.Clear();
            foreach (var filterWithState in presentation.Filters)
            {
                var filter = filterWithState.Filter;

                // Filters render as their icon (falling back to text when the filter has
                // none); the display text moves to the tooltip, as in VS.
                object content = filter.Image is { } image
                    && _viewElementFactory.CreateViewElement<Control>(_view, image) is { } icon
                    ? icon
                    : filter.DisplayText;
                if (content is Control iconControl)
                {
                    iconControl.Margin = default;
                }

                var toggle = new ToggleButton
                {
                    Content = content,
                    IsChecked = filterWithState.IsSelected,
                    IsEnabled = filterWithState.IsAvailable,
                    Padding = new Thickness(3.0),
                    MinWidth = 0.0,
                    MinHeight = 0.0,
                    Tag = filter,
                };
                ToolTip.SetTip(toggle, $"{filter.DisplayText} ({filter.AccessKey})");
                toggle.IsCheckedChanged += OnFilterToggled;
                _filtersPanel.Children.Add(toggle);

                // The expander toggle (e.g. "items from unimported namespaces") acts on the
                // list's contents rather than filtering it; a separator sets it apart.
                if (filter is CompletionExpander && !ReferenceEquals(filterWithState, presentation.Filters[^1]))
                {
                    _filtersPanel.Children.Add(new Border
                    {
                        Width = 1.0,
                        Background = _brushes.BorderBrush,
                        Margin = new Thickness(2.0, 1.0),
                    });
                }
            }

            _filtersPanel.Tag = presentation.Filters;
        }
        finally
        {
            _updatingFilters = false;
        }
    }

    /// <summary>
    /// The description pane beside the list (VS's "tooltip" for the selected item): fetched
    /// from the item's own source after a short debounce, rendered through the view element
    /// factories, and attached to the popup agent as adjunct content.
    /// </summary>
    private void ScheduleDescriptionUpdate()
    {
        _descriptionCancellation?.Cancel();
        var item = SelectedIndex >= 0 && SelectedIndex < VisibleItems.Count
            ? VisibleItems[SelectedIndex].CompletionItem
            : null;
        if (item is null || _session is null)
        {
            _describedItem = null;
            SetDescription(null);
            return;
        }

        if (ReferenceEquals(_describedItem, item))
        {
            return;
        }

        _descriptionCancellation = new CancellationTokenSource();
        _ = UpdateDescriptionAsync(_session, item, _descriptionCancellation.Token);
    }

    private async Task UpdateDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
    {
        try
        {
            // Debounce arrow-key runs through the list; the fetch and the UI mutation run
            // on the UI thread (the source contract and Avalonia both require it).
            await Task.Delay(150, token).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (token.IsCancellationRequested || !_isOpen)
                {
                    return;
                }

                var description = await item.Source.GetDescriptionAsync(session, item, token).ConfigureAwait(true);
                if (token.IsCancellationRequested || !_isOpen)
                {
                    return;
                }

                _describedItem = item;
                SetDescription(BuildDescriptionControl(description));
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            // A failing description never takes the list down; the pane just stays hidden.
            System.Diagnostics.Debug.WriteLine($"completion description failed: {ex}");
            Dispatcher.UIThread.Post(() => SetDescription(null));
        }
    }

    private Border? BuildDescriptionControl(object? description)
    {
        if (description is null or string { Length: 0 }
            || (description is string text && string.IsNullOrWhiteSpace(text))
            || _viewElementFactory.CreateViewElement<Control>(_view, description) is not { } element)
        {
            return null;
        }

        var border = new Border
        {
            Child = element,
            Background = _brushes.Background,
            BorderBrush = _brushes.BorderBrush,
            BorderThickness = new Thickness(1.0),
            Padding = new Thickness(8.0, 6.0),
            MaxWidth = 400.0,
            MaxHeight = 320.0,
            ClipToBounds = true,
        };
        border.SetValue(TextElement.ForegroundProperty, _brushes.Foreground);
        return border;
    }

    private void SetDescription(Control? control)
    {
        if (_agent is PopupAgent popup)
        {
            popup.AdjunctContent = control;
        }
    }

    private void OnFilterToggled(object? sender, EventArgs e)
    {
        if (_updatingFilters || sender is not ToggleButton toggle
            || _filtersPanel.Tag is not ImmutableArray<CompletionFilterWithState> filters)
        {
            return;
        }

        var updated = filters.Select(state => ReferenceEquals(state.Filter, toggle.Tag)
            ? state.WithSelected(toggle.IsChecked == true)
            : state).ToImmutableArray();
        FiltersChanged?.Invoke(this, new CompletionFilterChangedEventArgs(updated));
    }

    private void OnRowPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border { Tag: CompletionItem item })
        {
            CompletionItemSelected?.Invoke(this, new CompletionItemSelectedEventArgs(item, suggestionItemSelected: false));
        }
    }

    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { Tag: CompletionItem item })
        {
            CommitRequested?.Invoke(this, new CompletionItemEventArgs(item));
        }
    }
}
