using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.BraceMatching;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.QuickInfo;
using System;
using System.Threading;
using System.Threading.Tasks;
#if AVALONIA
using Avalonia.Media;
using Avalonia.Input;
using ModifierKeys = Avalonia.Input.InputModifiers;
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
        private CancellationTokenSource _braceMatchingCts;

        public RoslynCodeEditor()
        {
            _syncContext = SynchronizationContext.Current;
            _textMarkerService = new TextMarkerService(this);
            TextArea.TextView.LineTransformers.Add(_textMarkerService);
            TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            TextArea.Caret.PositionChanged += CaretOnPositionChanged;
        }

        public DocumentId Initialize(IRoslynHost roslynHost, IClassificationHighlightColors highlightColors, string workingDirectory, string documentText)
        {
            _roslynHost = roslynHost ?? throw new ArgumentNullException(nameof(roslynHost));
            _classificationHighlightColors = highlightColors ?? throw new ArgumentNullException(nameof(highlightColors));

            _braceMatcherHighlighter = new BraceMatcherHighlightRenderer(TextArea.TextView, _classificationHighlightColors);

            _quickInfoProvider = _roslynHost.GetService<IQuickInfoProvider>();
            _braceMatchingService = _roslynHost.GetService<IBraceMatchingService>();

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
            var result = await _braceMatchingService.GetAllMatchingBracesAsync(document, CaretOffset, token).ConfigureAwait(true);
            _braceMatcherHighlighter.SetHighlight(result.leftOfPosition, result.rightOfPosition);
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
            _textMarkerService.RemoveAll(x => true);

            foreach (var diagnosticData in args.Diagnostics)
            {
                if (diagnosticData.Severity == DiagnosticSeverity.Hidden || diagnosticData.IsSuppressed)
                {
                    continue;
                }

                var marker =
                    _textMarkerService.TryCreate(diagnosticData.TextSpan.Start, diagnosticData.TextSpan.Length);
                if (marker != null)
                {
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
