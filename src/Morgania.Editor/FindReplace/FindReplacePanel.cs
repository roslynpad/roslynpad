#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using System.Composition;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Creates a <see cref="FindReplacePanel"/> for every interactive text view. In VS the find
/// UI belongs to the shell, not the editor, so Morgania supplies an original implementation
/// over the editor core's search services (<see cref="ITextSearchService2"/> for match
/// enumeration, <see cref="ITextSearchNavigator3"/> for next/previous/replace).
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
public sealed class FindReplacePanelProvider : IWpfTextViewCreationListener
{
    private readonly ITextSearchService2 _searchService;
    private readonly ITextSearchNavigatorFactoryService _navigatorFactory;
    private readonly IEditorFormatMapService _editorFormatMaps;

    [ImportingConstructor]
    public FindReplacePanelProvider(
        ITextSearchService2 searchService,
        ITextSearchNavigatorFactoryService navigatorFactory,
        IEditorFormatMapService editorFormatMaps)
    {
        _searchService = searchService;
        _navigatorFactory = navigatorFactory;
        _editorFormatMaps = editorFormatMaps;
    }

    /// <summary>The layer the match highlights render on, under the text with the markers.</summary>
    [Export]
    [Name(FindReplacePanel.HighlightLayerName)]
    [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.TextMarker)]
    public AdornmentLayerDefinition? HighlightLayer { get; }

    public void TextViewCreated(IWpfTextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        textView.Properties.AddProperty(
            typeof(FindReplacePanel),
            new FindReplacePanel(
                textView,
                _searchService,
                _navigatorFactory.CreateSearchNavigator(textView.TextBuffer),
                _editorFormatMaps.GetEditorFormatMap(textView)));
    }
}

/// <summary>
/// The find/replace control, floating over the top-right corner of the text view (VS
/// placement): a search box with match-case / whole-word / regex toggles, a match count,
/// next/previous navigation, and a collapsible replace row. Hidden until a host calls
/// <see cref="Show"/> on the instance obtained via <see cref="Get"/>. All matches in the
/// viewport are highlighted on the panel's adornment layer; colors come from the
/// <see cref="FindReplaceFormatNames"/> editor-format-map entry.
/// </summary>
public sealed class FindReplacePanel
{
    public const string HighlightLayerName = "FindReplaceHighlight";

    private const int MaxCountedMatches = 1000;

    private readonly IWpfTextView _view;
    private readonly ITextSearchService2 _searchService;
    private readonly ITextSearchNavigator3 _navigator;
    private readonly IEditorFormatMap _formatMap;

    private readonly Border _root;
    private readonly TextBox _findBox;
    private readonly TextBox _replaceBox;
    private readonly ToggleButton _matchCase;
    private readonly ToggleButton _wholeWord;
    private readonly ToggleButton _useRegex;
    private readonly ToggleButton _replaceToggle;
    private readonly StackPanel _replaceRow;
    private readonly TextBlock _statusText;
    private readonly Avalonia.Controls.Shapes.Path _closeGlyph;

    private FindReplaceBrushes _brushes;
    private bool _isOpen;
    private bool _patternValid = true;
    private bool _isDisposed;

    internal FindReplacePanel(
        IWpfTextView view,
        ITextSearchService2 searchService,
        ITextSearchNavigator3 navigator,
        IEditorFormatMap formatMap)
    {
        _view = view;
        _searchService = searchService;
        _navigator = navigator;
        _formatMap = formatMap;
        _brushes = FindReplaceBrushes.Read(formatMap);

        _findBox = MakeInputBox("Find");
        _replaceBox = MakeInputBox("Replace");
        _matchCase = MakeToggle("Aa", "Match Case");
        _wholeWord = MakeToggle("ab", "Match Whole Word");
        _useRegex = MakeToggle(".*", "Use Regular Expression");
        _statusText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6.0, 0.0, 0.0, 0.0),
            FontSize = 11.0,
        };

        _closeGlyph = new Avalonia.Controls.Shapes.Path
        {
            Data = StreamGeometry.Parse("M 0,0 L 8,8 M 8,0 L 0,8"),
            StrokeThickness = 1.0,
        };
        var closeButton = MakeButton(_closeGlyph, "Close (Escape)", Hide);

        var findRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2.0 };
        findRow.Children.Add(_findBox);
        findRow.Children.Add(MakeButton(MakeIcon(FindReplaceIcons.FindPrevious), "Previous Match (Shift+Enter)", FindPrevious));
        findRow.Children.Add(MakeButton(MakeIcon(FindReplaceIcons.FindNext), "Next Match (Enter)", FindNext));
        findRow.Children.Add(closeButton);

        _replaceRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2.0, IsVisible = false };
        _replaceRow.Children.Add(_replaceBox);
        _replaceRow.Children.Add(MakeButton(MakeIcon(FindReplaceIcons.ReplaceNext), "Replace Next (Alt+R)", ReplaceNext));
        _replaceRow.Children.Add(MakeButton(MakeIcon(FindReplaceIcons.ReplaceAll), "Replace All (Alt+A)", ReplaceAll));

        var optionsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2.0 };
        optionsRow.Children.Add(_matchCase);
        optionsRow.Children.Add(_wholeWord);
        optionsRow.Children.Add(_useRegex);
        optionsRow.Children.Add(_statusText);

        var chevronIcon = MakeIcon(FindReplaceIcons.ExpandChevronDown);
        chevronIcon.RenderTransformOrigin = RelativePoint.Center;
        _replaceToggle = new ToggleButton
        {
            Content = chevronIcon,
            Padding = new Thickness(1.0),
            Margin = new Thickness(0.0, 0.0, 2.0, 0.0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0.0),
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
        };
        ToolTip.SetTip(_replaceToggle, "Toggle Replace");
        _replaceToggle.IsCheckedChanged += (_, _) =>
        {
            bool showReplace = _replaceToggle.IsChecked == true;
            chevronIcon.RenderTransform = showReplace ? new ScaleTransform(1.0, -1.0) : null;
            _replaceRow.IsVisible = showReplace;
        };

        var rows = new StackPanel { Spacing = 2.0 };
        rows.Children.Add(findRow);
        rows.Children.Add(_replaceRow);
        rows.Children.Add(optionsRow);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(4.0, 3.0) };
        Grid.SetColumn(_replaceToggle, 0);
        Grid.SetColumn(rows, 1);
        grid.Children.Add(_replaceToggle);
        grid.Children.Add(rows);

        _root = new Border
        {
            Child = grid,
            BorderThickness = new Thickness(1.0, 0.0, 1.0, 1.0),
            CornerRadius = new CornerRadius(0.0, 0.0, 3.0, 3.0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0.0, 0.0, 12.0, 0.0),
            IsVisible = false,
        };
        ApplyFormat();

        _findBox.TextChanged += (_, _) => UpdateMatches();
        _findBox.KeyDown += OnFindBoxKeyDown;
        _replaceBox.KeyDown += OnReplaceBoxKeyDown;
        _root.KeyDown += OnPanelKeyDown;
        foreach (var toggle in new[] { _matchCase, _wholeWord, _useRegex })
        {
            toggle.IsCheckedChanged += (_, _) => UpdateMatches();
        }

        _view.LayoutChanged += OnLayoutChanged;
        _view.VisualElement.KeyDown += OnViewKeyDown;
        _view.Closed += OnViewClosed;
        _formatMap.FormatMappingChanged += OnFormatMappingChanged;
    }

    /// <summary>Gets the panel of a view (created for every interactive "text" view).</summary>
    public static FindReplacePanel? Get(ITextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        return textView.Properties.TryGetProperty(typeof(FindReplacePanel), out FindReplacePanel panel) ? panel : null;
    }

    /// <summary>The term in the search box.</summary>
    public string? SearchText
    {
        get => _findBox.Text;
        set => _findBox.Text = value;
    }

    /// <summary>The term in the replace box.</summary>
    public string? ReplaceText
    {
        get => _replaceBox.Text;
        set => _replaceBox.Text = value;
    }

    /// <summary>Whether the panel is currently visible.</summary>
    public bool IsOpen => _isOpen;

    /// <summary>Opens the panel (optionally with the replace row), seeding it from the selection.</summary>
    public void Show(bool showReplace = false)
    {
        if (_isDisposed || _view.IsClosed || !EnsureAttached())
        {
            return;
        }

        _isOpen = true;
        _root.IsVisible = true;
        if (showReplace)
        {
            _replaceToggle.IsChecked = true;
        }

        var selection = _view.Selection.StreamSelectionSpan.SnapshotSpan;
        if (selection.Length is > 0 and <= 200 && selection.GetText() is { } text && !text.Contains('\n'))
        {
            _findBox.Text = text;
        }

        _navigator.ClearCurrentResult();
        _navigator.StartPoint = _view.Caret.Position.BufferPosition;
        UpdateMatches();

        // Focus once the panel has attached and measured.
        Dispatcher.UIThread.Post(() =>
        {
            _findBox.Focus();
            _findBox.SelectAll();
        });
    }

    /// <summary>Closes the panel and returns focus to the editor.</summary>
    public void Hide()
    {
        if (!_isOpen)
        {
            return;
        }

        _isOpen = false;
        _root.IsVisible = false;
        ClearHighlights();
        _view.VisualElement.Focus();
    }

    public void FindNext() => FindCore(reverse: false);

    public void FindPrevious() => FindCore(reverse: true);

    /// <summary>
    /// Replaces the current match and moves to the next one; when there is no current match
    /// yet, finds it first without replacing (so the first press shows what will change).
    /// </summary>
    public void ReplaceNext()
    {
        if (string.IsNullOrEmpty(_findBox.Text))
        {
            Show(showReplace: true);
            return;
        }

        if (_navigator.CurrentResult is null)
        {
            FindCore(reverse: false);
            return;
        }

        _navigator.ReplaceTerm = _replaceBox.Text ?? string.Empty;
        _navigator.SearchOptions = BuildOptions(reverse: false);
        try
        {
            _navigator.Replace();
        }
        catch (ArgumentException)
        {
            return;
        }

        FindCore(reverse: false);
    }

    /// <summary>Replaces every match in the buffer in a single edit (one undo step).</summary>
    public void ReplaceAll()
    {
        if (string.IsNullOrEmpty(_findBox.Text))
        {
            Show(showReplace: true);
            return;
        }

        var snapshot = _view.TextBuffer.CurrentSnapshot;
        var options = BuildOptions(reverse: false) & ~FindOptions.Wrap;
        int replaced = 0;
        try
        {
            using var edit = _view.TextBuffer.CreateEdit();
            foreach (var match in _searchService.FindAllForReplace(
                new SnapshotSpan(snapshot, 0, snapshot.Length), _findBox.Text, _replaceBox.Text ?? string.Empty, options))
            {
                edit.Replace(match.Item1.Span, match.Item2);
                replaced++;
            }

            if (replaced > 0)
            {
                edit.Apply();
            }
        }
        catch (ArgumentException)
        {
            // Invalid regex; the status text already says so.
            return;
        }

        _statusText.Text = replaced == 1 ? "Replaced 1 occurrence" : $"Replaced {replaced} occurrences";
    }

    /// <summary>
    /// Adds the panel over the text view's cell in the host grid: outside the view's zoom
    /// transform (the panel never scales) and fixed against scrolling, per VS placement.
    /// </summary>
    private bool EnsureAttached()
    {
        if (_root.Parent is not null)
        {
            return true;
        }

        if (!_view.Properties.TryGetProperty(typeof(IWpfTextViewHost), out IWpfTextViewHost host)
            || host is not WpfTextViewHost hostImplementation)
        {
            return false;
        }

        hostImplementation.AddViewOverlay(_root);
        return true;
    }

    private void FindCore(bool reverse)
    {
        if (string.IsNullOrEmpty(_findBox.Text))
        {
            Show();
            return;
        }

        _navigator.SearchTerm = _findBox.Text;
        _navigator.SearchOptions = BuildOptions(reverse);
        try
        {
            if (_navigator.Find() && _navigator.CurrentResult is { } result)
            {
                _view.Selection.Select(result, reverse);
                _view.Caret.MoveTo(reverse ? result.Start : result.End);
                _view.ViewScroller.EnsureSpanVisible(result);
            }
        }
        catch (ArgumentException)
        {
            // Invalid regex; the status text already says so.
        }

        RedrawHighlights();
    }

    private FindOptions BuildOptions(bool reverse)
    {
        var options = FindOptions.Wrap;
        if (_matchCase.IsChecked == true)
        {
            options |= FindOptions.MatchCase;
        }

        if (_wholeWord.IsChecked == true)
        {
            options |= FindOptions.WholeWord;
        }

        if (_useRegex.IsChecked == true)
        {
            options |= FindOptions.UseRegularExpressions;
        }

        if (reverse)
        {
            options |= FindOptions.SearchReverse;
        }

        return options;
    }

    private void UpdateMatches()
    {
        if (!_isOpen || _view.IsClosed)
        {
            return;
        }

        var term = _findBox.Text;
        _navigator.SearchTerm = term ?? string.Empty;
        _navigator.ClearCurrentResult();
        _patternValid = true;

        if (string.IsNullOrEmpty(term))
        {
            _statusText.Text = string.Empty;
            ClearHighlights();
            return;
        }

        int count = 0;
        try
        {
            var snapshot = _view.TextBuffer.CurrentSnapshot;
            count = _searchService
                .FindAll(new SnapshotSpan(snapshot, 0, snapshot.Length), term, BuildOptions(reverse: false) & ~FindOptions.Wrap)
                .Take(MaxCountedMatches)
                .Count();
        }
        catch (ArgumentException)
        {
            _patternValid = false;
        }

        _statusText.Text = !_patternValid ? "Invalid pattern"
            : count == 0 ? "No results"
            : count >= MaxCountedMatches ? $"{MaxCountedMatches}+ results"
            : count == 1 ? "1 result"
            : $"{count} results";
        _statusText.Foreground = _patternValid && count > 0 ? _brushes.Foreground : _brushes.NoMatchForeground;

        RedrawHighlights();
    }

    private void RedrawHighlights()
    {
        if (!_isOpen || _view.IsClosed || _view.InLayout || _view.TextViewLines is not { } lines)
        {
            return;
        }

        var layer = _view.GetAdornmentLayer(HighlightLayerName);
        layer.RemoveAllAdornments();

        var term = _findBox.Text;
        if (string.IsNullOrEmpty(term) || !_patternValid)
        {
            return;
        }

        var current = _navigator.CurrentResult;
        try
        {
            foreach (var match in _searchService
                .FindAll(lines.FormattedSpan, term, BuildOptions(reverse: false) & ~FindOptions.Wrap)
                .Take(MaxCountedMatches))
            {
                if (match.Length == 0
                    || !lines.IntersectsBufferSpan(match)
                    || lines.GetTextMarkerGeometry(match) is not { } geometry)
                {
                    continue;
                }

                bool isCurrent = current is { } currentSpan
                    && currentSpan.Snapshot == match.Snapshot
                    && currentSpan.Span == match.Span;
                var marker = new Avalonia.Controls.Shapes.Path
                {
                    Data = geometry,
                    Fill = isCurrent ? _brushes.CurrentMatchBackground : _brushes.MatchBackground,
                };

                // The geometry is in text-rendering coordinates; owner-controlled placement
                // with an explicit viewport offset (markers are rebuilt on every change).
                if (layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, match, tag: null, marker, removedCallback: null))
                {
                    Canvas.SetLeft(marker, -_view.ViewportLeft);
                    Canvas.SetTop(marker, -_view.ViewportTop);
                }
            }
        }
        catch (ArgumentException)
        {
        }
    }

    private void ClearHighlights()
    {
        if (!_view.IsClosed)
        {
            _view.GetAdornmentLayer(HighlightLayerName).RemoveAllAdornments();
        }
    }

    private void OnLayoutChanged(object? sender, TextViewLayoutChangedEventArgs e)
    {
        if (!_isOpen)
        {
            return;
        }

        if (e.OldSnapshot != e.NewSnapshot)
        {
            // An edit: the match set (and count) may have changed.
            UpdateMatches();
        }
        else
        {
            RedrawHighlights();
        }
    }

    private void OnViewKeyDown(object? sender, KeyEventArgs e)
    {
        if (_isOpen && e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnPanelKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key, e.KeyModifiers)
        {
            case (Key.Escape, KeyModifiers.None):
                Hide();
                e.Handled = true;
                break;
            case (Key.R, KeyModifiers.Alt):
                ReplaceNext();
                e.Handled = true;
                break;
            case (Key.A, KeyModifiers.Alt):
                ReplaceAll();
                e.Handled = true;
                break;
        }
    }

    private void OnFindBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                FindPrevious();
                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.None)
            {
                FindNext();
                e.Handled = true;
            }
        }
    }

    private void OnReplaceBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            ReplaceNext();
            e.Handled = true;
        }
    }

    private void OnFormatMappingChanged(object? sender, FormatItemsEventArgs e)
    {
        _brushes = FindReplaceBrushes.Read(_formatMap);
        ApplyFormat();
        RedrawHighlights();
    }

    private void OnViewClosed(object? sender, EventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _view.LayoutChanged -= OnLayoutChanged;
        _view.VisualElement.KeyDown -= OnViewKeyDown;
        _view.Closed -= OnViewClosed;
        _formatMap.FormatMappingChanged -= OnFormatMappingChanged;
        _navigator.Dispose();
    }

    private void ApplyFormat()
    {
        _root.Background = _brushes.Background;
        _root.BorderBrush = _brushes.BorderBrush;
        _root.SetValue(TextElement.ForegroundProperty, _brushes.Foreground);
        _closeGlyph.Stroke = _brushes.Foreground;
        foreach (var box in new[] { _findBox, _replaceBox })
        {
            box.Background = _brushes.InputBackground;
            box.Foreground = _brushes.InputForeground;
            box.BorderBrush = _brushes.InputBorder;
            box.CaretBrush = _brushes.InputForeground;
        }
    }

    private static Image MakeIcon(DrawingImage source) => new()
    {
        Source = source,
        Width = 16.0,
        Height = 16.0,
    };

    private static TextBox MakeInputBox(string watermark) => new()
    {
        Watermark = watermark,
        Width = 180.0,
        MinHeight = 24.0,
        Padding = new Thickness(6.0, 3.0),
        FontSize = 13.0,
        VerticalContentAlignment = VerticalAlignment.Center,
    };

    private static ToggleButton MakeToggle(string content, string tip)
    {
        var toggle = new ToggleButton
        {
            Content = content,
            FontSize = 11.0,
            Padding = new Thickness(4.0, 2.0),
            MinWidth = 26.0,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
        };
        ToolTip.SetTip(toggle, tip);
        return toggle;
    }

    private static Button MakeButton(Control content, string tip, Action action)
    {
        var button = new Button
        {
            Content = content,
            Padding = new Thickness(3.0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0.0),
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
        };
        ToolTip.SetTip(button, tip);
        button.Click += (_, _) => action();
        return button;
    }
}
