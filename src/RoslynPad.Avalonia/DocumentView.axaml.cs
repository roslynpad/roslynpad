using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.Document;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Editor;
using RoslynPad.Build;
using RoslynPad.UI;

namespace RoslynPad;

class DocumentView : UserControl, IDisposable
{
    private readonly RoslynCodeEditor _editor;

    public DocumentView()
    {
        AvaloniaXamlLoader.Load(this);

        _editor = this.FindControl<RoslynCodeEditor>("Editor") ?? throw new InvalidOperationException("Missing Editor");
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
        var viewModel = DataContext as OpenDocumentViewModel;
        if (viewModel == null) return;

        viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

        viewModel.EditorFocus += (o, e) => _editor.Focus();

        viewModel.MainViewModel.EditorFontSizeChanged += size => _editor.FontSize = size;
        _editor.FontSize = viewModel.MainViewModel.EditorFontSize;

        var documentText = await viewModel.LoadTextAsync().ConfigureAwait(true);

        var documentId = await _editor.InitializeAsync(viewModel.MainViewModel.RoslynHost,
            new ClassificationHighlightColors(),
            viewModel.WorkingDirectory, documentText, viewModel.SourceCodeKind).ConfigureAwait(true);

        viewModel.Initialize(documentId, OnError,
            () => new TextSpan(_editor.SelectionStart, _editor.SelectionLength),
            this);

        _editor.Document.TextChanged += (o, e) => viewModel.OnTextChanged();
    }

    private void NuGetOnPackageInstalled(PackageData package)
    {
        _ = this.GetDispatcher().InvokeAsync(() =>
        {
            var text = $"#r \"nuget: {package.Id}, {package.Version}\"{Environment.NewLine}";
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
