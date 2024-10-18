#pragma warning disable CS8618 

using Avalonia.Controls;
using AvaloniaEdit.Document;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Editor;
using RoslynPad.Build;
using RoslynPad.UI;
using Avalonia.Media;

namespace RoslynPad;

partial class DocumentView : UserControl, IDisposable
{
    private readonly RoslynCodeEditor _editor;
    private OpenDocumentViewModel? _viewModel;

    public DocumentView()
    {
        InitializeComponent();

        _editor = this.FindControl<RoslynCodeEditor>("Editor") ?? throw new InvalidOperationException("Missing Editor");

        DataContextChanged += OnDataContextChanged;

        //TODO: Add AvalonEditCommands ToggleAllFolds, ToggleFold
        //CommandBindings.Add(new CommandBinding(AvalonEditCommands.ToggleAllFolds, (s, e) => ToggleAllFoldings()));
        //CommandBindings.Add(new CommandBinding(AvalonEditCommands.ToggleFold, (s, e) => ToggleCurrentFolding()));
    }

    public OpenDocumentViewModel ViewModel => _viewModel.NotNull();

    private async void OnDataContextChanged(object? sender, EventArgs args)
    {
        if (DataContext is not OpenDocumentViewModel viewModel) return;
        _viewModel = viewModel;

        viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

        viewModel.EditorFocus += (o, e) => _editor.Focus();

        viewModel.MainViewModel.EditorFontSizeChanged += size => _editor.FontSize = size;
        viewModel.MainViewModel.ThemeChanged += OnThemeChanged;
        _editor.FontSize = viewModel.MainViewModel.EditorFontSize;
        SetFontFamily();

        var documentText = await viewModel.LoadTextAsync().ConfigureAwait(true);

        var documentId = await _editor.InitializeAsync(viewModel.MainViewModel.RoslynHost,
            new ThemeClassificationColors(viewModel.MainViewModel.Theme),
            viewModel.WorkingDirectory, documentText, viewModel.SourceCodeKind).ConfigureAwait(true);

        viewModel.Initialize(documentId, OnError,
            () => new TextSpan(_editor.SelectionStart, _editor.SelectionLength),
            this);

        _editor.Document.TextChanged += (o, e) => viewModel.OnTextChanged();

        void SetFontFamily()
        {
            var fonts = viewModel.MainViewModel.Settings.EditorFontFamily.Split(',');
            foreach (var font in fonts)
            {
                try
                {
                    _editor.FontFamily = FontFamily.Parse(font);
                    break;
                }
                catch
                {
                }
            }
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Editor.ClassificationHighlightColors = new ThemeClassificationColors(ViewModel.MainViewModel.Theme);
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
