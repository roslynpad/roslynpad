using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Controls;
using RoslynPad.Editor.Windows;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.BraceMatching;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Roslyn.QuickInfo;
using RoslynPad.Runtime;
using RoslynPad.UI;

namespace RoslynPad
{
    public partial class DocumentView : IDisposable
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ClassificationHighlightColors _classificationHighlightColors;
        private readonly TextMarkerService _textMarkerService;
        private readonly SynchronizationContext _syncContext;
        private readonly ErrorMargin _errorMargin;
        private readonly BraceMatcherHighlightRenderer _braceMatcherHighlighter;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private ContextActionsRenderer _contextActionsRenderer;
        private RoslynHost _roslynHost;
        private OpenDocumentViewModel _viewModel;
        private IQuickInfoProvider _quickInfoProvider;
        private CancellationTokenSource _braceMatchingCts;

        public DocumentView()
        {
            InitializeComponent();

            _classificationHighlightColors = new ClassificationHighlightColors();
            _textMarkerService = new TextMarkerService(Editor);
            _errorMargin = new ErrorMargin { Visibility = Visibility.Collapsed, MarkerBrush = TryFindResource("ExceptionMarker") as Brush, Width = 10 };
            _braceMatcherHighlighter = new BraceMatcherHighlightRenderer(Editor.TextArea.TextView, _classificationHighlightColors);
            Editor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            Editor.TextArea.TextView.LineTransformers.Add(_textMarkerService);
            Editor.TextArea.LeftMargins.Insert(0, _errorMargin);
            Editor.PreviewMouseWheel += EditorOnPreviewMouseWheel;
            Editor.TextArea.Caret.PositionChanged += CaretOnPositionChanged;

            _syncContext = SynchronizationContext.Current;

            DataContextChanged += OnDataContextChanged;
        }

        private async void CaretOnPositionChanged(object sender, EventArgs eventArgs)
        {
            _braceMatchingCts?.Cancel();

            Ln.Text = Editor.TextArea.Caret.Line.ToString();
            Col.Text = Editor.TextArea.Caret.Column.ToString();

            var braceMatchingService = _roslynHost?.GetService<IBraceMatchingService>();
            if (braceMatchingService == null) return;

            var cts = new CancellationTokenSource();
            var token = cts.Token;
            _braceMatchingCts = cts;

            var document = _roslynHost.GetDocument(_viewModel.DocumentId);
            var result = await braceMatchingService.GetAllMatchingBracesAsync(document, Editor.CaretOffset, token).ConfigureAwait(true);
            _braceMatcherHighlighter.SetHighlight(result.leftOfPosition, result.rightOfPosition);
        }

        private void TryJumpToBrace()
        {
            var caret = Editor.CaretOffset;

            if (TryJumpToPosition(_braceMatcherHighlighter.LeftOfPosition, caret) ||
                TryJumpToPosition(_braceMatcherHighlighter.RightOfPosition, caret))
            {
                Editor.ScrollToLine(Editor.TextArea.Caret.Line);
            }
        }

        private bool TryJumpToPosition(BraceMatchingResult? position, int caret)
        {
            if (position != null)
            {
                if (position.Value.LeftSpan.Contains(caret))
                {
                    Editor.CaretOffset = position.Value.RightSpan.End;
                    return true;
                }

                if (position.Value.RightSpan.Contains(caret) || position.Value.RightSpan.End == caret)
                {
                    Editor.CaretOffset = position.Value.LeftSpan.Start;
                    return true;
                }
            }

            return false;
        }

        private void EditorOnPreviewMouseWheel(object sender, MouseWheelEventArgs args)
        {
            if (_viewModel == null)
            {
                return;
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                _viewModel.MainViewModel.EditorFontSize += args.Delta > 0 ? 1 : -1;
                args.Handled = true;
            }
        }

        private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            _viewModel = (OpenDocumentViewModel)args.NewValue;
            _viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

            _viewModel.EditorFocus += (o, e) => Editor.Focus();

            _roslynHost = _viewModel.MainViewModel.RoslynHost;
            _quickInfoProvider = _roslynHost.GetService<IQuickInfoProvider>();

            _viewModel.MainViewModel.EditorFontSizeChanged += OnEditorFontSizeChanged;
            Editor.FontSize = _viewModel.MainViewModel.EditorFontSize;

            var avalonEditTextContainer = new AvalonEditTextContainer(Editor.Document) { Editor = Editor };

            await _viewModel.Initialize(
                avalonEditTextContainer,
                a => _syncContext.Post(o => ProcessDiagnostics(a), null),
                text => avalonEditTextContainer.UpdateText(text),
                OnError,
                () => new TextSpan(Editor.SelectionStart, Editor.SelectionLength),
                this).ConfigureAwait(true);

            var documentText = await _viewModel.LoadText().ConfigureAwait(true);
            Editor.AppendText(documentText);
            Editor.Document.UndoStack.ClearAll();
            Editor.Document.TextChanged += (o, e) => _viewModel.SetDirty();
            Editor.AsyncToolTipRequest = AsyncToolTipRequest;

            Editor.TextArea.TextView.LineTransformers.Insert(0, new RoslynHighlightingColorizer(_viewModel.DocumentId, _roslynHost, _classificationHighlightColors));

            _contextActionsRenderer = new ContextActionsRenderer(Editor, _textMarkerService);
            _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_viewModel.CommandProvider,
                _viewModel.DocumentId, _roslynHost));

            Editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_viewModel.DocumentId, _roslynHost);
        }

        private async Task AsyncToolTipRequest(ToolTipRequestEventArgs arg)
        {
            // TODO: consider invoking this with a delay, then showing the tool-tip without one
            var document = _roslynHost.GetDocument(_viewModel.DocumentId);
            var info = await _quickInfoProvider.GetItemAsync(document, arg.Position, CancellationToken.None).ConfigureAwait(true);
            if (info != null)
            {
                arg.SetToolTip(info.Create());
            }
        }

        private void OnError(ExceptionResultObject e)
        {
            if (e != null)
            {
                _errorMargin.Visibility = Visibility.Visible;
                _errorMargin.LineNumber = e.LineNumber;
                _errorMargin.Message = "Exception: " + e.Message;
            }
            else
            {
                _errorMargin.Visibility = Visibility.Collapsed;
            }
        }

        private void OnEditorFontSizeChanged(double fontSize)
        {
            Editor.FontSize = fontSize;
        }

        private void NuGetOnPackageInstalled(NuGetInstallResult installResult)
        {
            if (installResult.References.Count == 0) return;

            var text = string.Join(Environment.NewLine,
                installResult.References.Distinct().Select(r => Path.Combine(MainViewModel.NuGetPathVariableName, r))
                .Concat(installResult.FrameworkReferences.Distinct())
                .Where(r => !_roslynHost.HasReference(_viewModel.DocumentId, r))
                .Select(r => "#r \"" + r + "\"")) + Environment.NewLine;

            Dispatcher.InvokeAsync(() => Editor.Document.Insert(0, text, AnchorMovementType.Default));
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

                var marker = _textMarkerService.TryCreate(diagnosticData.TextSpan.Start, diagnosticData.TextSpan.Length);
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

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Key.T:
                        e.Handled = true;
                        NuGetSearch.Focus();
                        break;
                    case Key.OemCloseBrackets:
                        TryJumpToBrace();
                        break;
                }
            }
        }
        
        private void Editor_OnLoaded(object sender, RoutedEventArgs e)
        {
            Editor.Focus();
        }

        public void Dispose()
        {
            if (_viewModel?.MainViewModel != null)
            {
                _viewModel.MainViewModel.EditorFontSizeChanged -= OnEditorFontSizeChanged;
            }
        }

        private void OnTreeViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            {
                CopyToClipboard(sender);
            }
        }

        private void CopyClick(object sender, RoutedEventArgs e)
        {
            CopyToClipboard(sender);
        }

        private static void CopyToClipboard(object sender)
        {
            var element = (FrameworkElement)sender;
            var result = (ResultObject)element.DataContext;
            Clipboard.SetText(element.Tag as string == "All" ? result.ToString() : result.Value);
        }

        private void SearchTerm_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && _viewModel.NuGet.Packages?.Any() == true)
            {
                if (!_viewModel.NuGet.IsPackagesMenuOpen)
                {
                    _viewModel.NuGet.IsPackagesMenuOpen = true;
                }
                RootNuGetMenu.Focus();
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Editor.Focus();
            }
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ILViewerTab.IsSelected && ILViewerTab.Content == null)
            {
                var ilViewer = new ILViewer();
                ilViewer.SetBinding(ILViewer.TextProperty, nameof(_viewModel.ILText));
                ILViewerTab.Content = ilViewer;
            }
        }
    }
}
