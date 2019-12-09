using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.Document;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Editor;
using RoslynPad.Runtime;
using RoslynPad.UI;

namespace RoslynPad
{
    class DocumentView : UserControl, IDisposable
    {
        private readonly RoslynCodeEditor _editor;
        private OpenDocumentViewModel _viewModel;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public DocumentView()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            AvaloniaXamlLoader.Load(this);

            _editor = this.FindControl<RoslynCodeEditor>("Editor");

            _editor.FontFamily = GetPlatformFontFamily();

            DataContextChanged += OnDataContextChanged;
        }

        private static string GetPlatformFontFamily()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Consolas";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "Menlo";
            }
            else
            {
                return "Monospace";
            }
        }

        private async void OnDataContextChanged(object? sender, EventArgs args)
        {
            _viewModel = (OpenDocumentViewModel)DataContext;
            if (_viewModel == null) return;

            _viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

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

        private void NuGetOnPackageInstalled(PackageData package)
        {
            this.GetDispatcher().InvokeAsync(() =>
            {
                var text = $"#r \"nuget:{package.Id}/{package.Version}\"{Environment.NewLine}";
                _editor.Document.Insert(0, text, AnchorMovementType.Default);
            });
        }

        private void OnError(ExceptionResultObject? e)
        {
        }

        public void Dispose()
        {
        }
    }
}
