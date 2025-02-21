using Avalonia.Controls;
using AvaloniaEdit.Document;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Editor;
using RoslynPad.Build;
using RoslynPad.UI;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using DialogHostAvalonia;

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
    }

    public OpenDocumentViewModel ViewModel => _viewModel.NotNull();

    private async void OnDataContextChanged(object? sender, EventArgs args)
    {
        if (DataContext is not OpenDocumentViewModel viewModel) return;
        _viewModel = viewModel;

        viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

        viewModel.ReadInput += OnReadInput;
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

    private async void OnReadInput()
    {
        var textBox = new TextBox();

        var dialog = new HeaderedContentControl
        {
            Header = "Console Input",
            Content = textBox,
            Background = Brushes.White,
        };

        textBox.Loaded += (o, e) => textBox.Focus();

        textBox.KeyDown += (o, e) =>
        {
            if (e.Key == Key.Enter)
            {
                DialogHost.Close(MainWindow.DialogHostIdentifier);
            }
        };

        await DialogHost.Show(dialog, MainWindow.DialogHostIdentifier).ConfigureAwait(true);

        ViewModel.SendInput(textBox.Text ?? string.Empty);
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
