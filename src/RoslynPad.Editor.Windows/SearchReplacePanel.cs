using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit;
using Localization = ICSharpCode.AvalonEdit.Search.Localization;
using System.Collections.Generic;

namespace RoslynPad.Editor
{
    public class SearchReplacePanel : Control
    {
        private TextArea _textArea;
        private SearchReplaceInputHandler _handler;
        private TextDocument _currentDocument;
        private SearchReplaceResultBackgroundRenderer _renderer;
        private TextBox? _searchTextBox;
        private SearchReplacePanelAdorner _adorner;
        private ISearchStrategy _strategy;

        private ToolTip _messageView = new ToolTip { Placement = PlacementMode.Bottom, StaysOpen = true, Focusable = false };

        #region DependencyProperties
        /// <summary>
        /// Dependency property for <see cref="UseRegex"/>.
        /// </summary>
        public static readonly DependencyProperty UseRegexProperty =
            DependencyProperty.Register("UseRegex", typeof(bool), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should be interpreted as regular expression.
        /// </summary>
        public bool UseRegex
        {
            get => (bool)GetValue(UseRegexProperty);
            set { SetValue(UseRegexProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="MatchCase"/>.
        /// </summary>
        public static readonly DependencyProperty MatchCaseProperty =
            DependencyProperty.Register("MatchCase", typeof(bool), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should be interpreted case-sensitive.
        /// </summary>
        public bool MatchCase
        {
            get => (bool)GetValue(MatchCaseProperty);
            set { SetValue(MatchCaseProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="WholeWords"/>.
        /// </summary>
        public static readonly DependencyProperty WholeWordsProperty =
            DependencyProperty.Register("WholeWords", typeof(bool), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(false, SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets whether the search pattern should only match whole words.
        /// </summary>
        public bool WholeWords
        {
            get => (bool)GetValue(WholeWordsProperty);
            set { SetValue(WholeWordsProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="SearchPattern"/>.
        /// </summary>
        public static readonly DependencyProperty SearchPatternProperty =
            DependencyProperty.Register("SearchPattern", typeof(string), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata("", SearchPatternChangedCallback));

        /// <summary>
        /// Gets/sets the search pattern.
        /// </summary>
        public string SearchPattern
        {
            get => (string)GetValue(SearchPatternProperty);
            set { SetValue(SearchPatternProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="MarkerBrush"/>.
        /// </summary>
        public static readonly DependencyProperty MarkerBrushProperty =
            DependencyProperty.Register("MarkerBrush", typeof(Brush), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(Brushes.LightGreen, MarkerBrushChangedCallback));

        /// <summary>
        /// Gets/sets the Brush used for marking search results in the TextView.
        /// </summary>
        public Brush MarkerBrush
        {
            get => (Brush)GetValue(MarkerBrushProperty);
            set { SetValue(MarkerBrushProperty, value); }
        }

        /// <summary>
        /// Dependency property for <see cref="Localization"/>.
        /// </summary>
        public static readonly DependencyProperty LocalizationProperty =
            DependencyProperty.Register("Localization", typeof(Localization), typeof(SearchReplacePanel),
                new FrameworkPropertyMetadata(new Localization()));

        /// <summary>
        /// Gets/sets the localization for the SearchReplacePanel.
        /// </summary>
        public Localization Localization
        {
            get => (Localization)GetValue(LocalizationProperty);
            set { SetValue(LocalizationProperty, value); }
        }
        #endregion

        static void MarkerBrushChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchReplacePanel panel)
            {
                panel._renderer.MarkerBrush = (Brush)e.NewValue;
            }
        }

        static SearchReplacePanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchReplacePanel), new FrameworkPropertyMetadata(typeof(SearchReplacePanel)));
        }

        static void SearchPatternChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchReplacePanel panel)
            {
                panel.ValidateSearchText();
                panel.UpdateSearch();
            }
        }

        void UpdateSearch()
        {
            // only reset as long as there are results
            // if no results are found, the "no matches found" message should not flicker.
            // if results are found by the next run, the message will be hidden inside DoSearch ...
            if (_renderer.CurrentResults.Any())
                _messageView.IsOpen = false;
            var searchPattern = SearchPattern ?? "";
            _strategy = SearchStrategyFactory.Create(searchPattern, !MatchCase, WholeWords, UseRegex ? SearchMode.RegEx : SearchMode.Normal);
            OnSearchOptionsChanged(new SearchOptionsChangedEventArgs(searchPattern, MatchCase, UseRegex, WholeWords));
            DoSearch(true);
        }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        private SearchReplacePanel()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
        }

        /// <summary>
        /// Adds the commands used by SearchReplacePanel to the given CommandBindingCollection.
        /// </summary>
        public void RegisterCommands(CommandBindingCollection commandBindings)
        {
            _handler.RegisterGlobalCommands(commandBindings);
        }

        /// <summary>
        /// Removes the SearchReplacePanel from the TextArea.
        /// </summary>
        public void Uninstall()
        {
            CloseAndRemove();
            _textArea.DefaultInputHandler.NestedInputHandlers.Remove(_handler);
        }

        void AttachInternal(TextArea textArea)
        {
            _textArea = textArea;
            _adorner = new SearchReplacePanelAdorner(textArea, this);
            DataContext = this;

            _renderer = new SearchReplaceResultBackgroundRenderer();
            _currentDocument = textArea.Document;
            if (_currentDocument != null)
                _currentDocument.TextChanged += TextArea_Document_TextChanged;
            textArea.DocumentChanged += TextArea_DocumentChanged;
            KeyDown += SearchLayerKeyDown;

            CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, (sender, e) => FindNext()));
            CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, (sender, e) => FindPrevious()));
            CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, (sender, e) => Close()));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (sender, e) =>
            {
                IsReplaceMode = false;
                Reactivate();
            }));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, (sender, e) => IsReplaceMode = true));
            CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceNext, (sender, e) => ReplaceNext(), (sender, e) => e.CanExecute = IsReplaceMode));
            CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceAll, (sender, e) => ReplaceAll(), (sender, e) => e.CanExecute = IsReplaceMode));

            IsClosed = true;
        }

        void TextArea_DocumentChanged(object? sender, EventArgs e)
        {
            if (_currentDocument != null)
                _currentDocument.TextChanged -= TextArea_Document_TextChanged;
            _currentDocument = _textArea.Document;
            if (_currentDocument != null)
            {
                _currentDocument.TextChanged += TextArea_Document_TextChanged;
                DoSearch(false);
            }
        }

        void TextArea_Document_TextChanged(object? sender, EventArgs e)
        {
            DoSearch(false);
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _searchTextBox = Template.FindName("PART_searchTextBox", this) as TextBox;
        }

        void ValidateSearchText()
        {
            if (_searchTextBox == null)
                return;
            var be = _searchTextBox.GetBindingExpression(TextBox.TextProperty);
            try
            {
                Validation.ClearInvalid(be);
                UpdateSearch();
            }
            catch (SearchPatternException ex)
            {
                var ve = new ValidationError(be.ParentBinding.ValidationRules[0], be, ex.Message, ex);
                Validation.MarkInvalid(be, ve);
            }
        }

        /// <summary>
        /// Reactivates the SearchReplacePanel by setting the focus on the search box and selecting all text.
        /// </summary>
        public void Reactivate()
        {
            if (_searchTextBox == null)
                return;
            _searchTextBox.Focus();
            _searchTextBox.SelectAll();
        }

        /// <summary>
        /// Moves to the next occurrence in the file.
        /// </summary>
        public void FindNext()
        {
            var selectedResult = GetSelectedResult();
            var result = _renderer.CurrentResults.FirstOrDefault(r => r.Offset >= _textArea.Caret.Offset && r != selectedResult) ??
                         _renderer.CurrentResults.FirstOrDefault();

            if (result != null)
            {
                SelectResult(result);
            }
        }

        /// <summary>
        /// Moves to the previous occurrence in the file.
        /// </summary>
        public void FindPrevious()
        {
            var selectedResult = GetSelectedResult();
            var result = _renderer.CurrentResults.LastOrDefault(r => r.EndOffset <= _textArea.Caret.Offset && r != selectedResult) ??
                         _renderer.CurrentResults.LastOrDefault();


            if (result != null)
            {
                SelectResult(result);
            }
        }

        void DoSearch(bool changeSelection)
        {
            if (IsClosed)
                return;
            _renderer.CurrentResults.Clear();

            if (!string.IsNullOrEmpty(SearchPattern))
            {
                var offset = _textArea.Caret.Offset;
                if (changeSelection)
                {
                    _textArea.ClearSelection();
                }
                
                foreach (var result in _strategy.FindAll(_textArea.Document, 0, _textArea.Document.TextLength))
                {
                    if (changeSelection && result.Offset >= offset)
                    {
                        SelectResult(result);
                        changeSelection = false;
                    }
                    _renderer.CurrentResults.Add(result);
                }

                if (!_renderer.CurrentResults.Any())
                {
                    _messageView.IsOpen = true;
                    _messageView.Content = Localization.NoMatchesFoundText;
                    _messageView.PlacementTarget = _searchTextBox;
                }
                else
                    _messageView.IsOpen = false;
            }
            _textArea.TextView.InvalidateLayer(KnownLayer.Selection);
        }

        void SelectResult(ISearchResult searchResult)
        {
            _textArea.Caret.Offset = searchResult.Offset;
            _textArea.Selection = Selection.Create(_textArea, searchResult.Offset, searchResult.EndOffset);
            _textArea.Caret.BringCaretToView();
            // show caret even if the editor does not have the Keyboard Focus
            _textArea.Caret.Show();
        }

        void SearchLayerKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        FindPrevious();
                    else
                        FindNext();
                    if (_searchTextBox != null)
                    {
                        var error = Validation.GetErrors(_searchTextBox).FirstOrDefault();
                        if (error != null)
                        {
                            _messageView.Content = Localization.ErrorText + " " + error.ErrorContent;
                            _messageView.PlacementTarget = _searchTextBox;
                            _messageView.IsOpen = true;
                        }
                    }
                    break;
                case Key.Escape:
                    e.Handled = true;
                    Close();
                    break;
            }
        }

        /// <summary>
        /// Gets whether the Panel is already closed.
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Closes the SearchReplacePanel.
        /// </summary>
        public void Close()
        {
            var hasFocus = IsKeyboardFocusWithin;

            var layer = AdornerLayer.GetAdornerLayer(_textArea);
            if (layer != null)
                layer.Remove(_adorner);
            _messageView.IsOpen = false;
            _textArea.TextView.BackgroundRenderers.Remove(_renderer);
            if (hasFocus)
                _textArea.Focus();
            IsClosed = true;

            // Clear existing search results so that the segments don't have to be maintained
            _renderer.CurrentResults.Clear();
        }

        /// <summary>
        /// Closes the SearchReplacePanel and removes it.
        /// </summary>
        private void CloseAndRemove()
        {
            Close();
            _textArea.DocumentChanged -= TextArea_DocumentChanged;
            if (_currentDocument != null)
                _currentDocument.TextChanged -= TextArea_Document_TextChanged;
        }

        /// <summary>
        /// Opens the an existing search panel.
        /// </summary>
        public void Open()
        {
            if (!IsClosed) return;
            var layer = AdornerLayer.GetAdornerLayer(_textArea);
            if (layer != null)
                layer.Add(_adorner);
            _textArea.TextView.BackgroundRenderers.Add(_renderer);
            IsClosed = false;
            DoSearch(false);
        }

        /// <summary>
        /// Fired when SearchOptions are changed inside the SearchReplacePanel.
        /// </summary>
        public event EventHandler<SearchOptionsChangedEventArgs> SearchOptionsChanged;

        /// <summary>
        /// Raises the <see cref="SearchReplacePanel.SearchOptionsChanged" /> event.
        /// </summary>
        protected virtual void OnSearchOptionsChanged(SearchOptionsChangedEventArgs e)
        {
            SearchOptionsChanged?.Invoke(this, e);
        }

        public static readonly DependencyProperty IsReplaceModeProperty = DependencyProperty.Register(
            "IsReplaceMode", typeof(bool), typeof(SearchReplacePanel), new FrameworkPropertyMetadata());

        public bool IsReplaceMode
        {
            get => (bool)GetValue(IsReplaceModeProperty);
            set => SetValue(IsReplaceModeProperty, value);
        }

        public static readonly DependencyProperty ReplacePatternProperty = DependencyProperty.Register(
            "ReplacePattern", typeof(string), typeof(SearchReplacePanel), new FrameworkPropertyMetadata());

        public string ReplacePattern
        {
            get => (string)GetValue(ReplacePatternProperty);
            set => SetValue(ReplacePatternProperty, value);
        }

        /// <summary>
        /// Creates a SearchReplacePanel and installs it to the TextEditor's TextArea.
        /// </summary>
        public static SearchReplacePanel Install(TextEditor editor)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));
            return Install(editor.TextArea);
        }

        /// <summary>
        /// Creates a SearchReplacePanel and installs it to the TextArea.
        /// </summary>
        public static SearchReplacePanel Install(TextArea textArea)
        {
            if (textArea == null)
                throw new ArgumentNullException(nameof(textArea));
            var panel = new SearchReplacePanel { _textArea = textArea };
            panel.AttachInternal(textArea);
            panel._handler = new SearchReplaceInputHandler(textArea, panel);
            textArea.DefaultInputHandler.NestedInputHandlers.Add(panel._handler);
            return panel;
        }

        public void ReplaceNext()
        {
            if (!IsReplaceMode) return;

            var selectedResult = GetSelectedResult();
            if (selectedResult != null)
            {
                var replacement = selectedResult.ReplaceWith(ReplacePattern ?? string.Empty);
                _textArea.Selection.ReplaceSelectionWithText(replacement);
            }

            FindNext();
        }

        private ISearchResult? GetSelectedResult()
        {
            if (_textArea.Selection.IsEmpty)
                return null;

            var selectionStartOffset = _textArea.Document.GetOffset(_textArea.Selection.StartPosition.Location);
            var selectionLength = _textArea.Selection.Length;
            return _renderer.CurrentResults.FirstOrDefault(r => r.Offset == selectionStartOffset && r.Length == selectionLength);
        }

        public void ReplaceAll()
        {
            if (!IsReplaceMode) return;

            var document = _textArea.Document;
            using (document.RunUpdate())
            {
                var results = _renderer.CurrentResults.OrderByDescending(x => x.EndOffset).ToArray();
                foreach (var result in results)
                {
                    var replacement = result.ReplaceWith(ReplacePattern ?? string.Empty);
                    document.Replace(result.Offset, result.Length, new StringTextSource(replacement));
                }
            }
        }

        private class SearchReplaceInputHandler : TextAreaInputHandler
        {
            private readonly SearchReplacePanel _panel;

            internal SearchReplaceInputHandler(TextArea textArea, SearchReplacePanel panel)
                : base(textArea)
            {
                RegisterCommands();
                _panel = panel;
            }

            private void RegisterCommands()
            {
                CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
                CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, ExecuteReplace));
                CommandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceNext, ExecuteReplaceNext, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommandsEx.ReplaceAll, ExecuteReplaceAll, CanExecuteWithOpenSearchPanel));
                CommandBindings.Add(new CommandBinding(SearchCommands.CloseSearchPanel, ExecuteCloseSearchPanel, CanExecuteWithOpenSearchPanel));
            }

            private void ExecuteFind(object sender, ExecutedRoutedEventArgs e)
            {
                FindOrReplace(isReplaceMode: false);
            }

            private void ExecuteReplace(object sender, ExecutedRoutedEventArgs e)
            {
                FindOrReplace(isReplaceMode: true);
            }

            private void FindOrReplace(bool isReplaceMode)
            {
                _panel.IsReplaceMode = isReplaceMode;
                _panel.Open();
                if (!TextArea.Selection.IsEmpty && !TextArea.Selection.IsMultiline)
                    _panel.SearchPattern = TextArea.Selection.GetText();
                TextArea.Dispatcher.InvokeAsync(() => _panel.Reactivate(), DispatcherPriority.Input);
            }

            private void CanExecuteWithOpenSearchPanel(object sender, CanExecuteRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                {
                    e.CanExecute = false;
                    e.ContinueRouting = true;
                }
                else
                {
                    e.CanExecute = true;
                    e.Handled = true;
                }
            }

            private void ExecuteFindNext(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.FindNext();
                e.Handled = true;
            }

            private void ExecuteFindPrevious(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.FindPrevious();
                e.Handled = true;
            }

            private void ExecuteReplaceNext(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.ReplaceNext();
                e.Handled = true;
            }

            private void ExecuteReplaceAll(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.ReplaceAll();
                e.Handled = true;
            }

            private void ExecuteCloseSearchPanel(object sender, ExecutedRoutedEventArgs e)
            {
                if (_panel.IsClosed)
                    return;
                _panel.Close();
                e.Handled = true;
            }

            internal void RegisterGlobalCommands(CommandBindingCollection commandBindings)
            {
                commandBindings.Add(new CommandBinding(ApplicationCommands.Find, ExecuteFind));
                commandBindings.Add(new CommandBinding(SearchCommands.FindNext, ExecuteFindNext, CanExecuteWithOpenSearchPanel));
                commandBindings.Add(new CommandBinding(SearchCommands.FindPrevious, ExecuteFindPrevious, CanExecuteWithOpenSearchPanel));
            }
        }
    }

    /// <summary>
    /// EventArgs for <see cref="SearchReplacePanel.SearchOptionsChanged"/> event.
    /// </summary>
    public class SearchOptionsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the search pattern.
        /// </summary>
        public string SearchPattern { get; private set; }

        /// <summary>
        /// Gets whether the search pattern should be interpreted case-sensitive.
        /// </summary>
        public bool MatchCase { get; private set; }

        /// <summary>
        /// Gets whether the search pattern should be interpreted as regular expression.
        /// </summary>
        public bool UseRegex { get; private set; }

        /// <summary>
        /// Gets whether the search pattern should only match whole words.
        /// </summary>
        public bool WholeWords { get; private set; }

        /// <summary>
        /// Creates a new SearchOptionsChangedEventArgs instance.
        /// </summary>
        public SearchOptionsChangedEventArgs(string searchPattern, bool matchCase, bool useRegex, bool wholeWords)
        {
            SearchPattern = searchPattern;
            MatchCase = matchCase;
            UseRegex = useRegex;
            WholeWords = wholeWords;
        }
    }

    class SearchReplacePanelAdorner : Adorner
    {
        private SearchReplacePanel _panel;

        public SearchReplacePanelAdorner(TextArea textArea, SearchReplacePanel panel)
            : base(textArea)
        {
            _panel = panel;
            AddVisualChild(panel);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException();
            return _panel;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _panel.Arrange(new Rect(new Point(0, 0), finalSize));
            return new Size(_panel.ActualWidth, _panel.ActualHeight);
        }
    }

    class SearchReplaceResultBackgroundRenderer : IBackgroundRenderer
    {
        private Brush _markerBrush;
        private Pen _markerPen;

        public List<ISearchResult> CurrentResults { get; } = new List<ISearchResult>();

        public KnownLayer Layer => KnownLayer.Selection;

        public SearchReplaceResultBackgroundRenderer()
        {
            _markerBrush = Brushes.LightGreen;
            _markerPen = new Pen(_markerBrush, 1);
        }

        public Brush MarkerBrush
        {
            get => _markerBrush;
            set
            {
                _markerBrush = value;
                _markerPen = new Pen(_markerBrush, 1);
            }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");
            if (drawingContext == null)
                throw new ArgumentNullException("drawingContext");

            if (CurrentResults == null || !textView.VisualLinesValid)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            var viewStart = visualLines.First().FirstDocumentLine.Offset;
            var viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

            foreach (var result in CurrentResults.Where(r => viewStart <= r.Offset && r.Offset <= viewEnd || viewStart <= r.EndOffset && r.EndOffset <= viewEnd))
            {
                var geoBuilder = new BackgroundGeometryBuilder
                {
                    //BorderThickness = markerPen != null ? markerPen.Thickness : 0,
                    AlignToWholePixels = true,
                    CornerRadius = 3
                };
                geoBuilder.AddSegment(textView, result);
                var geometry = geoBuilder.CreateGeometry();
                if (geometry != null)
                {
                    drawingContext.DrawGeometry(_markerBrush, _markerPen, geometry);
                }
            }
        }
    }

    public static class SearchCommandsEx
    {
        /// <summary>Replaces the next occurrence in the document.</summary>
        public static readonly RoutedCommand ReplaceNext = new RoutedCommand("ReplaceNext", typeof(SearchReplacePanel),
            new InputGestureCollection
            {
                new KeyGesture(Key.R, ModifierKeys.Alt)
            });

        /// <summary>Replaces all the occurrences in the document.</summary>
        public static readonly RoutedCommand ReplaceAll = new RoutedCommand("ReplaceAll", typeof(SearchReplacePanel),
            new InputGestureCollection
            {
                new KeyGesture(Key.A, ModifierKeys.Alt)
            });
    }
}