using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.BraceMatching;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.QuickInfo;
using Microsoft.CodeAnalysis.Formatting;
using RoslynPad.Roslyn.Folding;
using System.Threading;
using Microsoft.CodeAnalysis.Structure;




#if AVALONIA
using AvaloniaEdit.Folding;
#endif

#if !AVALONIA
using ICSharpCode.AvalonEdit.Folding;
#endif


namespace RoslynPad.Editor;

public class RoslynCodeEditor : CodeTextEditor
{
    private readonly TextMarkerService _textMarkerService;
    private BraceMatcherHighlightRenderer? _braceMatcherHighlighter;
    private ContextActionsRenderer? _contextActionsRenderer;
    private IClassificationHighlightColors? _classificationHighlightColors;
    private IRoslynHost? _roslynHost;
    private DocumentId? _documentId;
    private IQuickInfoProvider? _quickInfoProvider;
    private IBraceMatchingService? _braceMatchingService;
    private CancellationTokenSource? _braceMatchingCts;
    private RoslynHighlightingColorizer? _colorizer;
    private FoldingBlockStructureProvider? _blockStructureService;

    public RoslynCodeEditor()
    {
        _textMarkerService = new TextMarkerService(this);
        TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
        TextArea.TextView.LineTransformers.Add(_textMarkerService);
        TextArea.Caret.PositionChanged += CaretOnPositionChanged;
        TextArea.TextView.Document.TextChanged += OnTextChanged;
    }

    private async void OnTextChanged(object? sender, EventArgs e) 
    {
        await RefreshFoldings().ConfigureAwait(false);
    }

    public FoldingManager? FoldingManager { get; private set; }


    public bool IsCodeFoldingEnabled
    {
        get { return (bool)this.GetValue(IsCodeFoldingEnabledProperty); }
        set { this.SetValue(IsCodeFoldingEnabledProperty, value); }
    }

    public bool IsBraceCompletionEnabled
    {
        get { return (bool)this.GetValue(IsBraceCompletionEnabledProperty); }
        set { this.SetValue(IsBraceCompletionEnabledProperty, value); }
    }

    public static readonly StyledProperty
#if AVALONIA
        <bool>
#endif
    IsCodeFoldingEnabledProperty =
    CommonProperty.Register<RoslynCodeEditor, bool>(nameof(IsCodeFoldingEnabledProperty), defaultValue: true);

    public static readonly StyledProperty
#if AVALONIA
        <bool>
#endif
        IsBraceCompletionEnabledProperty =
        CommonProperty.Register<RoslynCodeEditor, bool>(nameof(IsBraceCompletionEnabled), defaultValue: true);

    public static readonly StyledProperty
#if AVALONIA
        <ImageSource>
#endif
        ContextActionsIconProperty = CommonProperty.Register<RoslynCodeEditor, ImageSource>(
        nameof(ContextActionsIcon), onChanged: OnContextActionsIconChanged);

    private static void OnContextActionsIconChanged(RoslynCodeEditor editor, CommonPropertyChangedArgs<ImageSource> args)
    {
        if (editor._contextActionsRenderer != null)
        {
            editor._contextActionsRenderer.IconImage = args.NewValue;
        }
    }

    public ImageSource ContextActionsIcon
    {
        get => (ImageSource)this.GetValue(ContextActionsIconProperty);
        set => this.SetValue(ContextActionsIconProperty, value);
    }

    public IClassificationHighlightColors? ClassificationHighlightColors
    {
        get => _classificationHighlightColors;
        set
        {
            _classificationHighlightColors = value;
            if (_braceMatcherHighlighter is not null && value is not null)
            {
                _braceMatcherHighlighter.ClassificationHighlightColors = value;
            }

            RefreshHighlighting();
        }
    }

    public static readonly RoutedEvent CreatingDocumentEvent = CommonEvent.Register<RoslynCodeEditor, CreatingDocumentEventArgs>(nameof(CreatingDocument), RoutingStrategy.Bubble);

    public event EventHandler<CreatingDocumentEventArgs> CreatingDocument
    {
        add => AddHandler(CreatingDocumentEvent, value);
        remove => RemoveHandler(CreatingDocumentEvent, value);
    }

    protected virtual void OnCreatingDocument(CreatingDocumentEventArgs e)
    {
        RaiseEvent(e);
    }

    public async ValueTask<DocumentId> InitializeAsync(IRoslynHost roslynHost, IClassificationHighlightColors highlightColors, string workingDirectory, string documentText, SourceCodeKind sourceCodeKind)
    {
        _roslynHost = roslynHost ?? throw new ArgumentNullException(nameof(roslynHost));
        _classificationHighlightColors = highlightColors ?? throw new ArgumentNullException(nameof(highlightColors));

        _braceMatcherHighlighter = new BraceMatcherHighlightRenderer(TextArea.TextView, _classificationHighlightColors);

        _quickInfoProvider = _roslynHost.GetService<IQuickInfoProvider>();
        _braceMatchingService = _roslynHost.GetService<IBraceMatchingService>();

        var avalonEditTextContainer = new AvalonEditTextContainer(Document) { Editor = this };

        var creatingDocumentArgs = new CreatingDocumentEventArgs(avalonEditTextContainer);
        OnCreatingDocument(creatingDocumentArgs);

        _documentId = creatingDocumentArgs.DocumentId ??
            roslynHost.AddDocument(new DocumentCreationArgs(avalonEditTextContainer, workingDirectory, sourceCodeKind,
                avalonEditTextContainer.UpdateText));

        roslynHost.GetWorkspaceService<IDiagnosticsUpdater>(_documentId).DiagnosticsChanged += ProcessDiagnostics;

        if (roslynHost.GetDocument(_documentId) is { } document)
        {
            var options = await document.GetOptionsAsync().ConfigureAwait(true);
            Options.IndentationSize = options.GetOption(FormattingOptions.IndentationSize);
            Options.ConvertTabsToSpaces = !options.GetOption(FormattingOptions.UseTabs);
            _blockStructureService = new FoldingBlockStructureProvider();
        }

        AppendText(documentText);
        Document.UndoStack.ClearAll();
        AsyncToolTipRequest = OnAsyncToolTipRequest;

        _contextActionsRenderer = new ContextActionsRenderer(this, _textMarkerService) { IconImage = ContextActionsIcon };
        _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_documentId, _roslynHost));

        var completionProvider = new RoslynCodeEditorCompletionProvider(_documentId, _roslynHost);
        completionProvider.Warmup();

        CompletionProvider = completionProvider;

        RefreshHighlighting();

        #region  -- code folding --
        InstallFoldingManager();
        await RefreshFoldings().ConfigureAwait(false);
        #endregion  -- code folding --

        return _documentId;
    }

    public void RefreshHighlighting()
    {
        if (_colorizer != null)
        {
            TextArea.TextView.LineTransformers.Remove(_colorizer);
        }

        if (_documentId != null && _roslynHost != null && _classificationHighlightColors != null)
        {
            _colorizer = new RoslynHighlightingColorizer(_documentId, _roslynHost, _classificationHighlightColors);
            TextArea.TextView.LineTransformers.Insert(0, _colorizer);
        }
    }

    private async void CaretOnPositionChanged(object? sender, EventArgs eventArgs)
    {
        if (_roslynHost == null || _documentId == null || _braceMatcherHighlighter == null)
        {
            return;
        }

        _braceMatchingCts?.Cancel();

        if (_braceMatchingService == null)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        var token = cts.Token;
        _braceMatchingCts = cts;

        var document = _roslynHost.GetDocument(_documentId);
        if (document == null)
        {
            return;
        }

        try
        {
            var text = await document.GetTextAsync(token).ConfigureAwait(false);
            var caretOffset = CaretOffset;
            if (caretOffset <= text.Length)
            {
                var result = await _braceMatchingService.GetAllMatchingBracesAsync(document, caretOffset, token).ConfigureAwait(true);
                _braceMatcherHighlighter.SetHighlight(result.leftOfPosition, result.rightOfPosition);
            }
        }
        catch (OperationCanceledException)
        {
            // Caret moved again, we do nothing because execution stopped before propagating stale data
            // while fresh data is being applied in a different `CaretOnPositionChanged` handler which runs in parallel.
        }
    }

    private void TryJumpToBrace()
    {
        if (_braceMatcherHighlighter == null) return;

        var caret = CaretOffset;

        if (TryJumpToPosition(_braceMatcherHighlighter.LeftOfPosition, caret) ||
            TryJumpToPosition(_braceMatcherHighlighter.RightOfPosition, caret))
        {
            ScrollToLine(TextArea.Caret.Line);
        }
    }

    private bool TryJumpToPosition(BraceMatchingResult? position, int caret)
    {
        if (position != null)
        {
            if (position.Value.LeftSpan.Contains(caret))
            {
                CaretOffset = position.Value.RightSpan.End;
                return true;
            }

            if (position.Value.RightSpan.Contains(caret) || position.Value.RightSpan.End == caret)
            {
                CaretOffset = position.Value.LeftSpan.Start;
                return true;
            }
        }

        return false;
    }

    private async Task OnAsyncToolTipRequest(ToolTipRequestEventArgs arg)
    {
        if (_roslynHost == null || _documentId == null || _quickInfoProvider == null)
        {
            return;
        }

        // TODO: consider invoking this with a delay, then showing the tool-tip without one
        var document = _roslynHost.GetDocument(_documentId);
        if (document == null)
        {
            return;
        }

        var info = await _quickInfoProvider.GetItemAsync(document, arg.Position, CancellationToken.None).ConfigureAwait(true);
        if (info != null)
        {
            arg.SetToolTip(info.Create());
        }
    }

    protected async void ProcessDiagnostics(DiagnosticsChangedArgs args)
    {
        if (args.DocumentId != _documentId)
        {
            return;
        }

        await this.GetDispatcher();

        _textMarkerService.RemoveAll(d => d.Tag is DiagnosticData diagnosticData && args.RemovedDiagnostics.Contains(diagnosticData));

        if (_roslynHost == null || _documentId == null)
        {
            return;
        }

        var document = _roslynHost.GetDocument(_documentId);
        if (document == null || !document.TryGetText(out var sourceText))
        {
            return;
        }

        foreach (var diagnosticData in args.AddedDiagnostics)
        {
            if (diagnosticData.Severity == DiagnosticSeverity.Hidden || diagnosticData.IsSuppressed)
            {
                continue;
            }

            var span = diagnosticData.GetTextSpan(sourceText);
            if (span == null)
            {
                continue;
            }

            var marker = _textMarkerService.TryCreate(span.Value.Start, span.Value.Length);
            if (marker != null)
            {
                marker.Tag = diagnosticData;
                marker.MarkerColor = GetDiagnosticsColor(diagnosticData);
                marker.ToolTip = diagnosticData.Message;
            }
        }
    }

    private static Color GetDiagnosticsColor(DiagnosticData diagnosticData)
    {
        return diagnosticData.Severity switch
        {
            DiagnosticSeverity.Info => Colors.LimeGreen,
            DiagnosticSeverity.Warning => Colors.DodgerBlue,
            DiagnosticSeverity.Error => Colors.Red,
            _ => throw new ArgumentOutOfRangeException(nameof(diagnosticData)),
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.HasModifiers(ModifierKeys.Control))
        {
            switch (e.Key)
            {
                case Key.OemCloseBrackets:
                    TryJumpToBrace();
                    break;
            }
        }
    }

    #region  -- code folding --
    public async Task RefreshFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
            return;

        if (_documentId == null || _roslynHost == null || _blockStructureService == null)
            return;

        var document = _roslynHost.GetDocument(_documentId);
        if (document == null)
            return;

        var elements = await _blockStructureService.GetCodeFoldingsAsync(document, CancellationToken.None).ConfigureAwait(false);

        var foldings = elements.Select(s => new NewFolding { Name = s.Text, StartOffset = s.StartOffset, EndOffset = s.EndOffset }).ToList();
        var firstErrorOffset = 0;

        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

        FoldingManager?.UpdateFoldings(foldings, firstErrorOffset);
    }
    
    private void InstallFoldingManager()
    {
        if (!IsCodeFoldingEnabled)
            return;

        FoldingManager = FoldingManager.Install(TextArea);
    }

    private void UninstallFoldingManager()
    {
        FoldingManager.Uninstall(FoldingManager);
    }

    public void FoldAllFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
            return;

        foreach (var foldingSection in FoldingManager.AllFoldings)
            foldingSection.IsFolded = true;
    }

    public void UnfoldAllFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
            return;

        foreach (var foldingSection in FoldingManager.AllFoldings)
            foldingSection.IsFolded = false;
    }

    public void ToggleAllFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
            return;

        var fold = FoldingManager.AllFoldings.All(folding => !folding.IsFolded);

        foreach (var foldingSection in FoldingManager.AllFoldings)
            foldingSection.IsFolded = fold;
    }

    public void ToggleCurrentFolding()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
            return;
       

        var folding = FoldingManager.GetNextFolding(TextArea.Caret.Offset);
        if (folding == null || TextArea.Document.GetLocation(folding.StartOffset).Line != TextArea.Document.GetLocation(TextArea.Caret.Offset).Line)
        {
            folding = FoldingManager.GetFoldingsContaining(TextArea.Caret.Offset).LastOrDefault();
        }

        if (folding != null)
            folding.IsFolded = !folding.IsFolded;
    }

    public object? SaveFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
            return null;

        return FoldingManager?.AllFoldings
                              .Select(folding => new NewFolding
                              {
                                  StartOffset = folding.StartOffset,
                                  EndOffset = folding.EndOffset,
                                  Name = folding.Title,
                                  DefaultClosed = folding.IsFolded
                              })
                              .ToList();
    }

    public void RestoreFoldings(object foldings)
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
            return;

        var list = foldings as IEnumerable<NewFolding>;
        if (list == null)
            return;

        FoldingManager.Clear();
        FoldingManager.UpdateFoldings(list, -1);
    }
    #endregion  -- code folding --
}
