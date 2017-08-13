using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RoslynPad.Editor;
using System;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.QuickInfo;
using RoslynPad.UI;
using RoslynPad.Roslyn;
using Avalonia.Threading;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Runtime;

namespace RoslynPad
{
    class DocumentView : UserControl, IDisposable
    {
        private readonly CodeTextEditor _editor;
        private OpenDocumentViewModel _viewModel;
        private RoslynHost _roslynHost;
        private IQuickInfoProvider _quickInfoProvider;
        private IClassificationHighlightColors _classificationHighlightColors;
        private ContextActionsRenderer _contextActionsRenderer;
        private TextMarkerService _textMarkerService;

        public DocumentView()
        {
            AvaloniaXamlLoader.Load(this);

            _editor = this.FindControl<CodeTextEditor>("Editor");
            _classificationHighlightColors = new ClassificationHighlightColors();
            _textMarkerService = new TextMarkerService(_editor);

            DataContextChanged += OnDataContextChanged;
        }

        private async void OnDataContextChanged(object sender, EventArgs args)
        {
            _viewModel = DataContext as OpenDocumentViewModel;
            if (_viewModel == null) return;

            //_viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

            _viewModel.EditorFocus += (o, e) => _editor.Focus();

            _roslynHost = _viewModel.MainViewModel.RoslynHost;
            _quickInfoProvider = _roslynHost.GetService<IQuickInfoProvider>();

            //_viewModel.MainViewModel.EditorFontSizeChanged += OnEditorFontSizeChanged;
            //Editor.FontSize = _viewModel.MainViewModel.EditorFontSize;

            var avalonEditTextContainer = new AvalonEditTextContainer(_editor.Document) { Editor = _editor };

            await _viewModel.Initialize(
                avalonEditTextContainer,
                a => Dispatcher.UIThread.InvokeAsync(() => ProcessDiagnostics(a)),
                text => avalonEditTextContainer.UpdateText(text),
                OnError,
                () => new TextSpan(_editor.SelectionStart, _editor.SelectionLength),
                this).ConfigureAwait(true);

            var documentText = await _viewModel.LoadText().ConfigureAwait(true);
            _editor.AppendText(documentText);
            _editor.Document.UndoStack.ClearAll();
            _editor.Document.TextChanged += (o, e) => _viewModel.SetDirty();
            //_editor.AsyncToolTipRequest = AsyncToolTipRequest;

            _editor.TextArea.TextView.LineTransformers.Insert(0, new RoslynHighlightingColorizer(_viewModel.DocumentId, _roslynHost, _classificationHighlightColors));

            _contextActionsRenderer = new ContextActionsRenderer(_editor, _textMarkerService);
            _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_viewModel.CommandProvider,
                _viewModel.DocumentId, _roslynHost));

            _editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_viewModel.DocumentId, _roslynHost);
        }

        private void ProcessDiagnostics(DiagnosticsUpdatedArgs a)
        {
        }

        private void OnError(ExceptionResultObject e)
        {
        }

        public void Dispose()
        {
        }
    }
}
