using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RoslynPad.Editor;
using System;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.UI;
using RoslynPad.Roslyn;
using RoslynPad.Runtime;
using System.Runtime.InteropServices;

namespace RoslynPad
{
    class DocumentView : UserControl, IDisposable
    {
        private readonly RoslynCodeEditor _editor;
        private OpenDocumentViewModel _viewModel;

        public DocumentView()
        {
            AvaloniaXamlLoader.Load(this);

            _editor = this.FindControl<RoslynCodeEditor>("Editor");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _editor.FontFamily = "Consolas";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _editor.FontFamily = "Menlo";
            }
            else
            {
                _editor.FontFamily = "Monospace";
            }

            DataContextChanged += OnDataContextChanged;
        }

        private async void OnDataContextChanged(object sender, EventArgs args)
        {
            _viewModel = DataContext as OpenDocumentViewModel;
            if (_viewModel == null) return;

            //_viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

            _viewModel.EditorFocus += (o, e) => _editor.Focus();

            _viewModel.MainViewModel.EditorFontSizeChanged += size => _editor.FontSize = size;
            _editor.FontSize = _viewModel.MainViewModel.EditorFontSize;

            var documentText = await _viewModel.LoadText().ConfigureAwait(true);

            var documentId = _editor.Initialize(_viewModel.MainViewModel.RoslynHost,
                new ClassificationHighlightColors(),
                _viewModel.WorkingDirectory, documentText);

            _viewModel.Initialize(documentId, OnError,
                () => new TextSpan(_editor.SelectionStart, _editor.SelectionLength),
                this);

            _editor.Document.TextChanged += (o, e) => _viewModel.OnTextChanged();
        }

        private void OnError(ExceptionResultObject e)
        {
        }

        public void Dispose()
        {
        }
    }
}
