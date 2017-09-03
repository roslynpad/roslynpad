using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
#if AVALONIA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using Brush = Avalonia.Media.IBrush;
using MouseEventArgs = Avalonia.Input.PointerEventArgs;
using ModifierKeys = Avalonia.Input.InputModifiers;
using TextCompositionEventArgs = Avalonia.Input.TextInputEventArgs;
using RoutingStrategy = Avalonia.Interactivity.RoutingStrategies;
using CommandBinding = AvaloniaEdit.RoutedCommandBinding;
using AvalonEditCommands = AvaloniaEdit.AvaloniaEditCommands;
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
#endif

namespace RoslynPad.Editor
{
    public class CodeTextEditor : TextEditor
#if AVALONIA
        , IStyleable
#endif
    {
        private CustomCompletionWindow _completionWindow;
        private OverloadInsightWindow _insightWindow;
        private ToolTip _toolTip;

#if AVALONIA
        Type IStyleable.StyleKey => typeof(TextEditor);
#endif

        public CodeTextEditor()
        {
            Options = new TextEditorOptions
            {
                ConvertTabsToSpaces = true,
                AllowScrollBelowDocument = true,
                IndentationSize = 4,
                EnableEmailHyperlinks = false,
            };

            // TODO: remove this after bug fix
#if AVALONIA
            var lineMargin = new LineNumberMargin { Margin = new Thickness(0, 0, 10, 0) };
            lineMargin[~TextBlock.ForegroundProperty] = this[~LineNumbersForegroundProperty];
            TextArea.LeftMargins.Insert(0, lineMargin);
#else
            ShowLineNumbers = true;
#endif

            TextArea.TextView.VisualLinesChanged += OnVisualLinesChanged;
            TextArea.TextEntering += OnTextEntering;
            TextArea.TextEntered += OnTextEntered;

#if AVALONIA
            PointerHover += OnMouseHover;
            PointerHoverStopped += OnMouseHoverStopped;
#else
            MouseHover += OnMouseHover;
            MouseHoverStopped += OnMouseHoverStopped;

            ToolTipService.SetInitialShowDelay(this, 0);
            SearchReplacePanel.Install(this);
#endif

            var commandBindings = TextArea.CommandBindings;
            var deleteLineCommand = commandBindings.OfType<CommandBinding>().FirstOrDefault(x =>
                x.Command == AvalonEditCommands.DeleteLine);
            if (deleteLineCommand != null)
            {
                commandBindings.Remove(deleteLineCommand);
            }

            var contextMenu = new ContextMenu();
            contextMenu.SetItems(new[]
            {
                new MenuItem {Command = ApplicationCommands.Cut},
                new MenuItem {Command = ApplicationCommands.Copy},
                new MenuItem {Command = ApplicationCommands.Paste}
            });
            ContextMenu = contextMenu;
        }

        public static readonly StyledProperty<Brush> CompletionBackgroundProperty = CommonProperty.Register<CodeTextEditor, Brush>(
            nameof(CompletionBackground), CreateDefaultCompletionBackground());

        public bool IsCompletionWindowOpen
        {
            get => _completionWindow?.IsVisible == true;
        }

        public void CloseCompletionWindow()
        {
            if (_completionWindow != null)
            {
                _completionWindow.Close();
                _completionWindow = null;
            }
        }

        public bool IsInsightWindowOpen
        {
            get => _insightWindow?.IsVisible == true;
        }

        public void CloseInsightWindow()
        {
            if (_insightWindow != null)
            {
                _insightWindow.Close();
                _insightWindow = null;
            }
        }

        private static Brush CreateDefaultCompletionBackground()
        {
            return new SolidColorBrush(Color.FromRgb(240, 240, 240)).AsFrozen();
        }

        public Brush CompletionBackground
        {
            get => this.GetValue(CompletionBackgroundProperty);
            set => this.SetValue(CompletionBackgroundProperty, value);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Space && e.HasModifiers(ModifierKeys.Control))
            {
                e.Handled = true;
                var mode = e.HasModifiers(ModifierKeys.Shift)
                    ? TriggerMode.SignatureHelp
                    : TriggerMode.Completion;
                // ReSharper disable once UnusedVariable
                var task = ShowCompletion(mode);
            }
        }

        private enum TriggerMode
        {
            Text,
            Completion,
            SignatureHelp
        }

        public static readonly RoutedEvent ToolTipRequestEvent = CommonEvent.Register<CodeTextEditor, ToolTipRequestEventArgs>(
            nameof(ToolTipRequest), RoutingStrategy.Bubble);

        public Func<ToolTipRequestEventArgs, Task> AsyncToolTipRequest { get; set; }

        public event EventHandler<ToolTipRequestEventArgs> ToolTipRequest
        {
            add => AddHandler(ToolTipRequestEvent, value);
            remove => RemoveHandler(ToolTipRequestEvent, value);
        }

        private void OnVisualLinesChanged(object sender, EventArgs e)
        {
            _toolTip?.Close(this);
        }

        private void OnMouseHoverStopped(object sender, MouseEventArgs e)
        {
            if (_toolTip != null)
            {
                _toolTip.Close(this);
                e.Handled = true;
            }
        }

        private async void OnMouseHover(object sender, MouseEventArgs e)
        {
            TextViewPosition? position;
            try
            {
                position = TextArea.TextView.GetPositionFloor(e.GetPosition(TextArea.TextView) + TextArea.TextView.ScrollOffset);
            }
            catch (ArgumentOutOfRangeException)
            {
                // TODO: check why this happens
                e.Handled = true;
                return;
            }
            var args = new ToolTipRequestEventArgs { InDocument = position.HasValue };
            if (!position.HasValue || position.Value.Location.IsEmpty)
            {
                return;
            }

            args.LogicalPosition = position.Value.Location;
            args.Position = Document.GetOffset(position.Value.Line, position.Value.Column);

            RaiseEvent(args);

            if (args.ContentToShow == null)
            {
                var asyncRequest = AsyncToolTipRequest?.Invoke(args);
                if (asyncRequest != null)
                {
                    await asyncRequest.ConfigureAwait(true);
                }
            }

            if (args.ContentToShow == null) return;

            if (_toolTip == null)
            {
                _toolTip = new ToolTip { MaxWidth = 400 };
#if !AVALONIA
                _toolTip.Closed += (o, a) => _toolTip = null;
                ToolTipService.SetInitialShowDelay(_toolTip, 0);
                _toolTip.PlacementTarget = this; // required for property inheritance
#endif
            }

            if (args.ContentToShow is string stringContent)
            {
                _toolTip.SetContent(this, new TextBlock
                {
                    Text = stringContent,
                    TextWrapping = TextWrapping.Wrap
                });
            }
            else
            {
                _toolTip.SetContent(this, args.ContentToShow);
            }

            e.Handled = true;
            _toolTip.Open(this);
#if AVALONIA
            _toolTip.InvalidateVisual();
#endif
        }

        #region Open & Save File

        public void OpenFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            _completionWindow?.Close();
            _insightWindow?.Close();

            Load(fileName);
            Document.FileName = fileName;

            SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(fileName));
        }

        public bool SaveFile()
        {
            if (string.IsNullOrEmpty(Document.FileName))
            {
                return false;
            }

            Save(Document.FileName);
            return true;
        }

        #endregion

        #region Code Completion

        public ICodeEditorCompletionProvider CompletionProvider { get; set; }

        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            // ReSharper disable once UnusedVariable
            var task = ShowCompletion(TriggerMode.Text);
        }

        private async Task ShowCompletion(TriggerMode triggerMode)
        {
            if (CompletionProvider == null)
            {
                return;
            }

            int offset;
            GetCompletionDocument(out offset);
            var completionChar = triggerMode == TriggerMode.Text ? Document.GetCharAt(offset - 1) : (char?)null;
            var results = await CompletionProvider.GetCompletionData(offset, completionChar,
                        triggerMode == TriggerMode.SignatureHelp).ConfigureAwait(true);
            if (results.OverloadProvider != null)
            {
                results.OverloadProvider.Refresh();

                if (_insightWindow.IsOpen())
                {
                    _insightWindow.Provider = results.OverloadProvider;
                }
                else
                {
                    _insightWindow = new OverloadInsightWindow(TextArea)
                    {
                        Provider = results.OverloadProvider,
                        Background = CompletionBackground,
                        // TODO: style
#if !AVALONIA
                        Style = TryFindResource(typeof(InsightWindow)) as Style
#endif
                    };

                    _insightWindow.Closed += (o, args) => _insightWindow = null;
                    _insightWindow.Show();
                }
                return;
            }

            if (!_completionWindow.IsOpen() && results.CompletionData?.Any() == true)
            {
                _insightWindow?.Close();

                // Open code completion after the user has pressed dot:
                _completionWindow = new CustomCompletionWindow(TextArea)
                {
                    MinWidth = 300,
#if !AVALONIA
                    Background = CompletionBackground,
#endif
                    CloseWhenCaretAtBeginning = triggerMode == TriggerMode.Completion || triggerMode == TriggerMode.Text,
                    UseHardSelection = results.UseHardSelection,
                };

                if (completionChar != null && char.IsLetterOrDigit(completionChar.Value))
                {
                    _completionWindow.StartOffset -= 1;
                }

                var data = _completionWindow.CompletionList.CompletionData;
                ICompletionDataEx selected = null;
                foreach (var completion in results.CompletionData)
                {
                    if (completion.IsSelected)
                    {
                        selected = completion;
                    }
                    data.Add(completion);
                }

                _completionWindow.CompletionList.SelectedItem = selected;

                _completionWindow.Closed += (o, args) => { _completionWindow = null; };
                _completionWindow.Show();
            }
        }

        private void OnTextEntering(object sender, TextCompositionEventArgs args)
        {
            if (args.Text.Length > 0 && _completionWindow != null)
            {
                if (!char.IsLetterOrDigit(args.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    _completionWindow.CompletionList.RequestInsertion(args);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        /// <summary>
        /// Gets the document used for code completion, can be overridden to provide a custom document
        /// </summary>
        /// <param name="offset"></param>
        /// <returns>The document of this text editor.</returns>
        protected virtual IDocument GetCompletionDocument(out int offset)
        {
            offset = CaretOffset;
            return Document;
        }

        #endregion

        private class CustomCompletionWindow : CompletionWindow
        {
            private bool _isSoftSelectionActive;
            private KeyEventArgs _keyDownArgs;

            public CustomCompletionWindow(TextArea textArea) : base(textArea)
            {
                _isSoftSelectionActive = true;
                CompletionList.SelectionChanged += CompletionListOnSelectionChanged;
                CompletionList.ListBox.SetBorderThickness(
// TODO: find a better way
#if AVALONIA
                    1
#else
                    0
#endif
                    );

#if AVALONIA
                CompletionList.ListBox.PointerPressed +=
#else
                CompletionList.ListBox.PreviewMouseDown +=
#endif
                    (o, e) => _isSoftSelectionActive = false;
            }

#if AVALONIA
            protected override void DetachEvents()
            {
                // TODO: temporary workaround until SetParent(null) is removed
                var selected = CompletionList.SelectedItem;
                base.DetachEvents();
                CompletionList.SelectedItem = selected;
            }
#endif

            private void CompletionListOnSelectionChanged(object sender, SelectionChangedEventArgs args)
            {
                if (!UseHardSelection &&
                    _isSoftSelectionActive && _keyDownArgs?.Handled != true
                    && args.AddedItems?.Count > 0)
                {
                    CompletionList.SelectedItem = null;
                }
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.Key == Key.Home || e.Key == Key.End) return;

                _keyDownArgs = e;

                base.OnKeyDown(e);

                SetSoftSelection(e);
            }

            private void SetSoftSelection(RoutedEventArgs e)
            {
                if (e.Handled)
                {
                    _isSoftSelectionActive = false;
                }
            }

            // ReSharper disable once MemberCanBePrivate.Local
            public bool UseHardSelection { get; set; }
        }
    }
}
