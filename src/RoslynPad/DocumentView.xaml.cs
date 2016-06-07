using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.RoslynEditor;
using RoslynPad.Runtime;

namespace RoslynPad
{
    public partial class DocumentView : IDisposable
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly TextMarkerService _textMarkerService;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private ContextActionsRenderer _contextActionsRenderer;
        private readonly SynchronizationContext _syncContext;
        private RoslynHost _roslynHost;
        private OpenDocumentViewModel _viewModel;

        public DocumentView()
        {
            InitializeComponent();

            _textMarkerService = new TextMarkerService(Editor);
            Editor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            Editor.TextArea.TextView.LineTransformers.Add(_textMarkerService);
            Editor.PreviewMouseWheel += EditorOnPreviewMouseWheel;
            Editor.TextArea.Caret.PositionChanged += CaretOnPositionChanged;

            _syncContext = SynchronizationContext.Current;

            DataContextChanged += OnDataContextChanged;
        }

        private void CaretOnPositionChanged(object sender, EventArgs eventArgs)
        {
            Ln.Text = Editor.TextArea.Caret.Line.ToString();
            Col.Text = Editor.TextArea.Caret.Column.ToString();
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
            _roslynHost = _viewModel.MainViewModel.RoslynHost;

            _viewModel.MainViewModel.EditorFontSizeChanged += OnEditorFontSizeChanged;
            Editor.FontSize = _viewModel.MainViewModel.EditorFontSize;

            var avalonEditTextContainer = new AvalonEditTextContainer(Editor);

            await _viewModel.Initialize(
                avalonEditTextContainer,
                a => _syncContext.Post(o => ProcessDiagnostics(a), null),
                text => avalonEditTextContainer.UpdateText(text),
                this).ConfigureAwait(true);

            var documentText = await _viewModel.LoadText().ConfigureAwait(true);
            Editor.AppendText(documentText);
            Editor.Document.UndoStack.ClearAll();
            Editor.Document.TextChanged += (o, e) => _viewModel.SetDirty(Editor.Document.TextLength);

            Editor.TextArea.TextView.LineTransformers.Insert(0, new RoslynHighlightingColorizer(_viewModel.DocumentId, _roslynHost));

            _contextActionsRenderer = new ContextActionsRenderer(Editor, _textMarkerService);
            _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_viewModel.DocumentId, _roslynHost));

            Editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_viewModel.DocumentId, _roslynHost);
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

        private void Editor_OnKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.R && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            //{
            //    _roslynHost.GetService<IInlineRenameService>().StartInlineSession(
            //        _roslynHost.CurrentDocument, new TextSpan(Editor.CaretOffset, 1));
            //}
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
            var element = (FrameworkElement) sender;
            var result = (ResultObject) element.DataContext;
            Clipboard.SetText(result.ToString());
        }
    }
}
