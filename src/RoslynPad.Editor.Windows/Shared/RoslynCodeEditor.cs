using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.BraceMatching;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.Formatting;
using RoslynPad.Roslyn.Structure;
using RoslynPad.Roslyn.QuickInfo;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Buffers;
using TextChange = Microsoft.CodeAnalysis.Text.TextChange;

namespace RoslynPad.Editor;

public class RoslynCodeEditor : CodeTextEditor
{
    private static readonly SearchValues<char> s_formattingTriggerChars = SearchValues.Create(";{}#nte:)");
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
    private IBlockStructureService? _blockStructureService;
    private ICodeFormattingService? _codeFormattingService;
    private SnippetManager? _snippetManager;

    public RoslynCodeEditor()
    {
        _textMarkerService = new TextMarkerService(this);
        TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
        TextArea.TextView.LineTransformers.Add(_textMarkerService);
        TextArea.Caret.PositionChanged += CaretOnPositionChanged;
        TextArea.TextEntered += OnRoslynTextEntered;

        Observable.FromEventPattern<EventHandler, EventArgs>(
            h => TextArea.TextView.Document.TextChanged += h,
            h => TextArea.TextView.Document.TextChanged -= h)
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => RefreshFoldings().ConfigureAwait(true));
    }

    private async void OnTextChanged(object? sender, EventArgs e)
    {
        await RefreshFoldings().ConfigureAwait(true);
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
        _snippetManager = new SnippetManager();

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

            _blockStructureService = document.GetLanguageService<IBlockStructureService>();
            _codeFormattingService = document.GetLanguageService<ICodeFormattingService>();
        }

        TextArea.IndentationStrategy = new RoslynIndentationStrategy(roslynHost, _documentId);

        AppendText(documentText);
        Document.UndoStack.ClearAll();
        AsyncToolTipRequest = OnAsyncToolTipRequest;

        _contextActionsRenderer = new ContextActionsRenderer(this, _textMarkerService) { IconImage = ContextActionsIcon };
        _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_documentId, _roslynHost));

        var completionProvider = new RoslynCodeEditorCompletionProvider(_documentId, _roslynHost);
        completionProvider.Warmup();

        CompletionProvider = completionProvider;

        RefreshHighlighting();

        InstallFoldingManager();
        await RefreshFoldings().ConfigureAwait(true);

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
            var text = await document.GetTextAsync(token).ConfigureAwait(true);
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

        var info = await _quickInfoProvider.GetItemAsync(document, arg.Position).ConfigureAwait(true);
        if (info?.Create() is { } content)
        {
            arg.SetToolTip(content);
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
                marker.Priority = (int)diagnosticData.Severity;
                marker.ToolTip = $"{diagnosticData.Id}: {diagnosticData.Message}";
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
            _ => Colors.LimeGreen,
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

    private void OnRoslynTextEntered(object? sender, TextCompositionEventArgs e)
    {
        FormatOnCharTyped();
    }

    private void FormatOnCharTyped()
    {
        if (_roslynHost == null || _documentId == null || _codeFormattingService == null)
        {
            return;
        }

        var caretOffset = CaretOffset;
        var typedChar = Document.GetCharAt(caretOffset - 1);
        if (!s_formattingTriggerChars.Contains(typedChar))
        {
            return;
        }

        var document = _roslynHost.GetDocument(_documentId);
        if (document == null)
        {
            return;
        }


        try
        {
            var parsedDocument = ParsedDocument.CreateSynchronously(document);

            if (!_codeFormattingService.ShouldFormatOnTypedCharacter(parsedDocument, typedChar, caretOffset, CancellationToken.None))
            {
                return;
            }

            var changes = _codeFormattingService.GetFormattingChangesOnTypedCharacter(parsedDocument, caretOffset, CancellationToken.None);
            if (changes.IsDefaultOrEmpty)
            {
                return;
            }

            var newCaretOffset = TrackPositionThroughChanges(caretOffset, changes);

            using (Document.RunUpdate())
            {
                // Apply changes in reverse order so earlier offsets remain valid
                foreach (var change in changes.OrderByDescending(c => c.Span.Start))
                {
                    Document.Replace(change.Span.Start, change.Span.Length,
                        new StringTextSource(change.NewText ?? string.Empty));
                }
            }

            CaretOffset = Math.Min(newCaretOffset, Document.TextLength);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Tracks a caret position forward through text changes using negative tracking.
    /// When the position is at the boundary of an insertion, it stays before the inserted text.
    /// Changes must be sorted by Span.Start and non-overlapping.
    /// </summary>
    private static int TrackPositionThroughChanges(int position, ImmutableArray<TextChange> changes)
    {
        var delta = 0;
        foreach (var change in changes)
        {
            if (position < change.Span.Start)
            {
                break;
            }

            if (position <= change.Span.End)
            {
                // Position is within [Start, End] of this change (includes zero-length insertions
                // at the caret position). Negative tracking: stay at start of new text.
                return change.Span.Start + delta;
            }

            delta += (change.NewText?.Length ?? 0) - change.Span.Length;
        }

        return position + delta;
    }

    protected override async Task<bool> TryExpandSnippetAsync()
    {
        if (_snippetManager == null)
        {
            return false;
        }

        // Get the word before the caret
        var offset = CaretOffset;
        if (offset == 0)
        {
            return false;
        }

        var document = Document;
        var line = document.GetLineByOffset(offset);
        var lineText = document.GetText(line.Offset, offset - line.Offset);

        // Extract snippet text from the line
        var result = SnippetExpandHelper.ExtractSnippetTextFromLine(lineText, lineText.Length);
        if (result == null)
        {
            return false; // No word found
        }

        var (wordStart, _) = result.Value;
        var snippetStartOffset = line.Offset + wordStart;
        var snippetLength = offset - snippetStartOffset;

        // Get the Roslyn document if available
        Document? roslynDocument = null;
        if (_roslynHost != null && _documentId != null)
        {
            roslynDocument = _roslynHost.GetDocument(_documentId);
        }

        // Expand the snippet (extraction, class name resolution happens internally)
        return await SnippetExpandHelper.ExpandSnippetAsync(
            _snippetManager,
            TextArea,
            snippetStartOffset,
            snippetLength,
            roslynDocument).ConfigureAwait(false);
    }

    public async Task RefreshFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
        {
            return;
        }

        if (_documentId == null || _roslynHost == null || _blockStructureService == null)
        {
            return;
        }

        var document = _roslynHost.GetDocument(_documentId);
        if (document == null)
        {
            return;
        }

        try
        {
            var elements = await _blockStructureService.GetBlockStructureAsync(document).ConfigureAwait(true);

            var foldings = elements.Spans
                .Select(s => new NewFolding { Name = s.BannerText, StartOffset = s.TextSpan.Start, EndOffset = s.TextSpan.End })
                .OrderBy(item => item.StartOffset);

            FoldingManager?.UpdateFoldings(foldings, firstErrorOffset: 0);
        }
        catch
        {
        }
    }

    private void InstallFoldingManager()
    {
        if (!IsCodeFoldingEnabled)
        {
            return;
        }

        FoldingManager = FoldingManager.Install(TextArea);
    }

    public void FoldAllFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
        {
            return;
        }

        foreach (var foldingSection in FoldingManager.AllFoldings)
        {
            foldingSection.IsFolded = true;
        }
    }

    public void UnfoldAllFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
        {
            return;
        }

        foreach (var foldingSection in FoldingManager.AllFoldings)
            foldingSection.IsFolded = false;
    }

    public void ToggleAllFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
        {
            return;
        }

        var fold = FoldingManager.AllFoldings.All(folding => !folding.IsFolded);

        foreach (var foldingSection in FoldingManager.AllFoldings)
            foldingSection.IsFolded = fold;
    }

    public void ToggleCurrentFolding()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
        {
            return;
        }

        var folding = FoldingManager.GetNextFolding(TextArea.Caret.Offset);
        if (folding == null || TextArea.Document.GetLocation(folding.StartOffset).Line != TextArea.Document.GetLocation(TextArea.Caret.Offset).Line)
        {
            folding = FoldingManager.GetFoldingsContaining(TextArea.Caret.Offset).LastOrDefault();
        }

        if (folding != null)
            folding.IsFolded = !folding.IsFolded;
    }

    public IEnumerable<NewFolding> SaveFoldings()
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
        {
            return [];
        }

        return FoldingManager?.AllFoldings
            .Select(folding => new NewFolding
            {
                StartOffset = folding.StartOffset,
                EndOffset = folding.EndOffset,
                Name = folding.Title,
                DefaultClosed = folding.IsFolded
            })
            .ToList() ?? [];
    }

    public void RestoreFoldings(IEnumerable<NewFolding> foldings)
    {
        if (FoldingManager == null || !IsCodeFoldingEnabled)
        {
            return;
        }


        FoldingManager.Clear();
        FoldingManager.UpdateFoldings(foldings, -1);
    }
}
