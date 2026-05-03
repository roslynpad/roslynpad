using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Editor;
using RoslynPad.Build;
using RoslynPad.UI;
using RoslynPad.Utilities;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using DialogHostAvalonia;
using AvaloniaEdit;

namespace RoslynPad;

partial class DocumentView : UserControl, IDisposable
{
    private readonly RoslynCodeEditor _editor;
    private readonly TextBox _nuGetSearch;
    private readonly TextBlock _lnTextBlock;
    private readonly TextBlock _colTextBlock;
    private OpenDocumentViewModel? _viewModel;

    public DocumentView()
    {
        InitializeComponent();

        _editor = this.FindControl<RoslynCodeEditor>("Editor") ?? throw new InvalidOperationException("Missing Editor");
        _nuGetSearch = this.FindControl<TextBox>("NuGetSearch") ?? throw new InvalidOperationException("Missing NuGetSearch");
        _lnTextBlock = this.FindControl<TextBlock>("Ln") ?? throw new InvalidOperationException("Missing Ln");
        _colTextBlock = this.FindControl<TextBlock>("Col") ?? throw new InvalidOperationException("Missing Col");

        _editor.TextArea.Caret.PositionChanged += CaretOnPositionChanged;
        _nuGetSearch.KeyDown += NuGetSearch_OnKeyDown;

        DataContextChanged += OnDataContextChanged;
    }

    public OpenDocumentViewModel ViewModel => _viewModel.NotNull();

    private void CaretOnPositionChanged(object? sender, EventArgs e)
    {
        _lnTextBlock.Text = _editor.TextArea.Caret.Line.ToString(CultureInfo.InvariantCulture);
        _colTextBlock.Text = _editor.TextArea.Caret.Column.ToString(CultureInfo.InvariantCulture);
    }

    private async void OnDataContextChanged(object? sender, EventArgs args)
    {
        if (DataContext is not OpenDocumentViewModel viewModel) return;
        _viewModel = viewModel;

        InitializeKeyBindings(viewModel);

        viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

        viewModel.ReadInput += OnReadInput;
        viewModel.EditorFocus += (o, e) => _editor.Focus();
        viewModel.FindRequested += (o, e) => ApplicationCommands.Find.Execute(null, _editor.TextArea);
        viewModel.FindReplaceRequested += (o, e) => ApplicationCommands.Replace.Execute(null, _editor.TextArea);
        viewModel.DocumentUpdated += (o, e) =>
        {
            Dispatcher.UIThread.Post(() => _editor.RefreshHighlighting());
            Dispatcher.UIThread.Post(async () => await _editor.RefreshFoldings().ConfigureAwait(true));
        };

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

    private void InitializeKeyBindings(OpenDocumentViewModel viewModel)
    {
        this.AddKeyBinding(KeyBindingCommands.RunScript, viewModel.RunCommand);
        this.AddKeyBinding(KeyBindingCommands.TerminateRunningScript, viewModel.TerminateCommand);
        this.AddKeyBinding(KeyBindingCommands.SaveDocument, viewModel.SaveCommand);
        this.AddKeyBinding(KeyBindingCommands.FormatDocument, viewModel.FormatDocumentCommand);
        this.AddKeyBinding(KeyBindingCommands.CommentSelection, viewModel.CommentSelectionCommand);
        this.AddKeyBinding(KeyBindingCommands.UncommentSelection, viewModel.UncommentSelectionCommand);
        this.AddKeyBinding(KeyBindingCommands.RenameSymbol, viewModel.RenameSymbolCommand);
        this.AddKeyBinding(KeyBindingCommands.SearchNuGet, new DelegateCommand(() => _nuGetSearch.Focus()));
    }

    private void NuGetSearch_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && ViewModel.NuGet.Packages?.Any() == true)
        {
            if (!ViewModel.NuGet.IsPackagesMenuOpen)
            {
                ViewModel.NuGet.IsPackagesMenuOpen = true;
            }
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            _editor.Focus();
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
            var text = ViewModel.FormatPackageReference(package.Id, package.Version);
            _editor.Document.Insert(0, text, AnchorMovementType.Default);
        });
    }

    private void OnError(ExceptionResultObject? e)
    {
    }

    public void Dispose()
    {
        _editor.TextArea.Caret.PositionChanged -= CaretOnPositionChanged;
    }
}
