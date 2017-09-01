using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.BraceMatching;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.QuickInfo;
using System;
using System.Threading;
using System.Threading.Tasks;
using RoslynPad.Roslyn.AutomaticCompletion;
#if AVALONIA
using Avalonia.Media;
using Avalonia.Input;
using ModifierKeys = Avalonia.Input.InputModifiers;
using TextCompositionEventArgs = Avalonia.Input.TextInputEventArgs;
#else
using System.Windows.Media;
using System.Windows.Input;
#endif

namespace RoslynPad.Editor
{
    public class RoslynCodeEditor : CodeTextEditor
    {
        private readonly SynchronizationContext _syncContext;
        private readonly TextMarkerService _textMarkerService;
        private BraceMatcherHighlightRenderer _braceMatcherHighlighter;
        private ContextActionsRenderer _contextActionsRenderer;
        private IClassificationHighlightColors _classificationHighlightColors;
        private IRoslynHost _roslynHost;
        private DocumentId _documentId;
        private IQuickInfoProvider _quickInfoProvider;
        private IBraceMatchingService _braceMatchingService;
        private IBraceCompletionProvider _braceCompletionProvider;
        private CancellationTokenSource _braceMatchingCts;

        public RoslynCodeEditor()
        {
            _syncContext = SynchronizationContext.Current;
            _textMarkerService = new TextMarkerService(this);
            TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            TextArea.TextView.LineTransformers.Add(_textMarkerService);
            TextArea.Caret.PositionChanged += CaretOnPositionChanged;
            TextArea.TextEntered += OnTextEntered;
        }

        private void OnTextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length == 1)
            {
                _braceCompletionProvider.TryComplete(_roslynHost.GetDocument(_documentId), CaretOffset);
            }
        }

        public DocumentId Initialize(IRoslynHost roslynHost, IClassificationHighlightColors highlightColors, string workingDirectory, string documentText)
        {
            _roslynHost = roslynHost ?? throw new ArgumentNullException(nameof(roslynHost));
            _classificationHighlightColors = highlightColors ?? throw new ArgumentNullException(nameof(highlightColors));

            _braceMatcherHighlighter = new BraceMatcherHighlightRenderer(TextArea.TextView, _classificationHighlightColors);

            _quickInfoProvider = _roslynHost.GetService<IQuickInfoProvider>();
            _braceMatchingService = _roslynHost.GetService<IBraceMatchingService>();
            _braceCompletionProvider = _roslynHost.GetService<IBraceCompletionProvider>();

            var avalonEditTextContainer = new AvalonEditTextContainer(Document) { Editor = this };

            _documentId = roslynHost.AddDocument(avalonEditTextContainer, workingDirectory,
                args => _syncContext.Post(o => ProcessDiagnostics(args), null),
                text => avalonEditTextContainer.UpdateText(text));

            AppendText(documentText);
            Document.UndoStack.ClearAll();
            AsyncToolTipRequest = OnAsyncToolTipRequest;

            TextArea.TextView.LineTransformers.Insert(0, new RoslynHighlightingColorizer(_documentId, _roslynHost, _classificationHighlightColors));

            _contextActionsRenderer = new ContextActionsRenderer(this, _textMarkerService);
            _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_documentId, _roslynHost));

            CompletionProvider = new RoslynCodeEditorCompletionProvider(_documentId, _roslynHost);
            
            return _documentId;
        }

        private async void CaretOnPositionChanged(object sender, EventArgs eventArgs)
        {
            _braceMatchingCts?.Cancel();

            if (_braceMatchingService == null) return;

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            _braceMatchingCts = cts;

            var document = _roslynHost.GetDocument(_documentId);
            var text = await document.GetTextAsync().ConfigureAwait(false);
            var caretOffset = CaretOffset;
            if (caretOffset <= text.Length)
            {
                var result = await _braceMatchingService.GetAllMatchingBracesAsync(document, caretOffset, token).ConfigureAwait(true);
                _braceMatcherHighlighter.SetHighlight(result.leftOfPosition, result.rightOfPosition);
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
            // TODO: consider invoking this with a delay, then showing the tool-tip without one
            var document = _roslynHost.GetDocument(_documentId);
            var info = await _quickInfoProvider.GetItemAsync(document, arg.Position, CancellationToken.None).ConfigureAwait(true);
            if (info != null)
            {
                arg.SetToolTip(info.Create());
            }
        }

        private void ProcessDiagnostics(DiagnosticsUpdatedArgs args)
        {
            _textMarkerService.RemoveAll(marker => Equals(args.Id, marker.Tag));

            if (args.Kind != DiagnosticsUpdatedKind.DiagnosticsCreated)
            {
                return;
            }

            foreach (var diagnosticData in args.Diagnostics)
            {
                if (diagnosticData.Severity == DiagnosticSeverity.Hidden || diagnosticData.IsSuppressed)
                {
                    continue;
                }

                var marker = _textMarkerService.TryCreate(diagnosticData.TextSpan.Start, diagnosticData.TextSpan.Length);
                if (marker != null)
                {
                    marker.Tag = args.Id;
                    marker.MarkerColor = GetDiagnosticsColor(diagnosticData);
                    marker.ToolTip = diagnosticData.Message;
                }
            }
        }

        private static Color GetDiagnosticsColor(DiagnosticData diagnosticData)
        {
            switch (diagnosticData.Severity)
            {
                case DiagnosticSeverity.Info:
                    return Colors.LimeGreen;
                case DiagnosticSeverity.Warning:
                    return Colors.DodgerBlue;
                case DiagnosticSeverity.Error:
                    return Colors.Red;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
    }
}
