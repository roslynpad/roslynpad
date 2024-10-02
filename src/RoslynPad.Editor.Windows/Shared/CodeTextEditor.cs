using System.IO;

namespace RoslynPad.Editor;

public partial class CodeTextEditor : TextEditor
{
    private CodeEditorCompletionWindow? _completionWindow;
    private CodeEditorOverloadInsightWindow? _insightWindow;
    private ToolTip? _toolTip;

    public CodeTextEditor()
    {
        ShowLineNumbers = true;

        Options = new TextEditorOptions
        {
            ConvertTabsToSpaces = true,
            AllowScrollBelowDocument = true,
            IndentationSize = 4,
            EnableEmailHyperlinks = false,
            ShowBoxForControlCharacters = true,
        };

        TextArea.TextView.VisualLinesChanged += OnVisualLinesChanged;
        TextArea.TextEntering += OnTextEntering;
        TextArea.TextEntered += OnTextEntered;

        var commandBindings = TextArea.CommandBindings;
        var deleteLineCommand = commandBindings.OfType<CommandBinding>().FirstOrDefault(x =>
            x.Command == AvalonEditCommands.DeleteLine);
        if (deleteLineCommand != null)
        {
            commandBindings.Remove(deleteLineCommand);
        }

        var contextMenu = new ContextMenu
        {
            ItemsSource = new[]
            {
                new MenuItem { Command = ApplicationCommands.Cut },
                new MenuItem { Command = ApplicationCommands.Copy },
                new MenuItem { Command = ApplicationCommands.Paste }
            }
        };
        ContextMenu = contextMenu;

        Initialize();
    }

    partial void Initialize();

    public bool IsCompletionWindowOpen => _completionWindow?.IsVisible == true;

    public void CloseCompletionWindow()
    {
        if (_completionWindow != null)
        {
            _completionWindow.Close();
            _completionWindow = null;
        }
    }

    public bool IsInsightWindowOpen => _insightWindow?.IsVisible == true;

    public void CloseInsightWindow()
    {
        if (_insightWindow != null)
        {
            _insightWindow.Close();
            _insightWindow = null;
        }
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
            _ = ShowCompletion(mode);
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

    public Func<ToolTipRequestEventArgs, Task>? AsyncToolTipRequest { get; set; }

    public event EventHandler<ToolTipRequestEventArgs> ToolTipRequest
    {
        add => AddHandler(ToolTipRequestEvent, value);
        remove => RemoveHandler(ToolTipRequestEvent, value);
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e)
    {
        _toolTip?.Close(this);
    }

    private void OnMouseHoverStopped(object? sender, MouseEventArgs e)
    {
        if (_toolTip != null)
        {
            _toolTip.Close(this);
            e.Handled = true;
        }
    }

    private async void OnMouseHover(object? sender, MouseEventArgs e)
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
        if (!position.HasValue || position.Value.Location.IsEmpty || position.Value.IsAtEndOfLine)
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

        if (args.ContentToShow == null)
        {
            return;
        }

        if (_toolTip == null)
        {
            _toolTip = new ToolTip { MaxWidth = 400 };
            InitializeToolTip();
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
            _toolTip.SetContent(this, new ContentPresenter
            {
                Content = args.ContentToShow,
                MaxWidth = 400
            });
        }

        e.Handled = true;
        _toolTip.Open(this);

        AfterToolTipOpen();
    }

    partial void InitializeToolTip();
    partial void AfterToolTipOpen();

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

    public ICodeEditorCompletionProvider? CompletionProvider { get; set; }

    private void OnTextEntered(object? sender, TextCompositionEventArgs e)
    {
        _ = ShowCompletion(TriggerMode.Text);
    }

    private async Task ShowCompletion(TriggerMode triggerMode)
    {
        if (CompletionProvider == null)
        {
            return;
        }

        GetCompletionDocument(out var offset);
        var completionChar = triggerMode == TriggerMode.Text ? Document.GetCharAt(offset - 1) : (char?)null;
        var results = await CompletionProvider.GetCompletionData(offset, completionChar,
                    triggerMode == TriggerMode.SignatureHelp).ConfigureAwait(true);
        if (results.OverloadProvider != null)
        {
            results.OverloadProvider.Refresh();

            if (_insightWindow != null && _insightWindow.IsOpen())
            {
                _insightWindow.Provider = results.OverloadProvider;
            }
            else
            {
                _insightWindow = new CodeEditorOverloadInsightWindow(TextArea)
                {
                    Provider = results.OverloadProvider,
                };

                InitializeInsightWindow();

                _insightWindow.Closed += (o, args) => _insightWindow = null;
                _insightWindow.Show();
            }
            return;
        }

        if (_completionWindow?.IsOpen() != true && results.CompletionData != null && results.CompletionData.Any())
        {
            _insightWindow?.Close();

            // Open code completion after the user has pressed dot:
            _completionWindow = new CodeEditorCompletionWindow(TextArea)
            {
                MinWidth = 300,
                CloseWhenCaretAtBeginning = triggerMode == TriggerMode.Completion || triggerMode == TriggerMode.Text,
                UseHardSelection = results.UseHardSelection,
            };

            InitializeCompletionWindow();

            if (completionChar != null && char.IsLetterOrDigit(completionChar.Value))
            {
                _completionWindow.StartOffset -= 1;
            }

            var data = _completionWindow.CompletionList.CompletionData;
            ICompletionDataEx? selected = null;
            foreach (var completion in results.CompletionData)
            {
                if (completion.IsSelected)
                {
                    selected = completion;
                }

                data.Add(completion);
            }

            try
            {
                _completionWindow.CompletionList.SelectedItem = selected;
            }
            catch (Exception)
            {
                // TODO-AV: Fix this in AvaloniaEdit
            }

            _completionWindow.Closed += (o, args) => { _completionWindow = null; };
            _completionWindow.Show();
        }
    }

    partial void InitializeInsightWindow();

    partial void InitializeCompletionWindow();

    private void OnTextEntering(object? sender, TextCompositionEventArgs args)
    {
        if (args.Text?.Length > 0 && _completionWindow != null)
        {
            if (!IsCharIdentifier(args.Text[0]))
            {
                // Whenever no identifier letter is typed while the completion window is open,
                // insert the currently selected element.
                _completionWindow.CompletionList.RequestInsertion(args);
            }
        }
        // Do not set e.Handled=true.
        // We still want to insert the character that was typed.
    }
    /// <summary>
    /// Checks if a provided char is a well-known identifier
    /// </summary>
    /// <param name="c">The charcater to check</param>
    /// <returns><c>true</c> if <paramref name="c"/> is a well-known identifier.</returns>
    private bool IsCharIdentifier(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
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
}
