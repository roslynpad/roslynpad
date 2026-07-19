using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using DialogHostAvalonia;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using RoslynPad.Build;
using RoslynPad.Editor;
using RoslynPad.UI;
using RoslynPad.Utilities;

namespace RoslynPad;

partial class DocumentView : UserControl, IDisposable
{
    private readonly TextBox _nuGetSearch;
    private readonly CodeEditorView _editor;

    private IWpfTextView? _textView;
    private ITextBuffer? _buffer;

    public DocumentView()
    {
        InitializeComponent();

        _editor = this.FindControl<CodeEditorView>("Editor") ?? throw new InvalidOperationException("Missing Editor");
        _nuGetSearch = this.FindControl<TextBox>("NuGetSearch") ?? throw new InvalidOperationException("Missing NuGetSearch");

        _nuGetSearch.KeyDown += NuGetSearch_OnKeyDown;

        DataContextChanged += OnDataContextChanged;
    }

    public OpenDocumentViewModel ViewModel { get => field.NotNull(); private set; }

    private void CaretOnPositionChanged(object? sender, CaretPositionChangedEventArgs e)
    {
        var position = e.NewPosition.BufferPosition;
        var line = position.GetContainingLine();
        ViewModel.CurrentLine = line.LineNumber + 1;
        ViewModel.CurrentColumn = position.Position - line.Start.Position + 1;
    }

    private async void OnDataContextChanged(object? sender, EventArgs args)
    {
        if (DataContext is not OpenDocumentViewModel viewModel) return;
        ViewModel = viewModel;

        InitializeKeyBindings(viewModel);

        viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

        viewModel.ReadInput += OnReadInput;
        viewModel.EditorFocus += (o, e) => FocusEditor();
        viewModel.NavigationRequested += span => _editor.NavigateToSpan(span);
        viewModel.FindRequested += (o, e) => FindReplace?.Show(showReplace: false);
        viewModel.FindReplaceRequested += (o, e) => FindReplace?.Show(showReplace: true);

        var documentText = await viewModel.LoadTextAsync().ConfigureAwait(true);

        var roslynHost = viewModel.MainViewModel.RoslynHost;

        Morgania.CodeAnalysis.Editor.DiagnosticsSquiggles.DisabledDiagnostics = roslynHost.DisabledDiagnostics;

        var buffer = _editor.CreateBuffer(viewModel.MainViewModel, documentText);
        _buffer = buffer;

        var documentId = roslynHost.AddDocument(new RoslynPad.Roslyn.DocumentCreationArgs(
            buffer.AsTextContainer(),
            viewModel.WorkingDirectory,
            viewModel.SourceCodeKind,
            OnTextUpdated,
            viewModel.Document?.Name));

        var textView = _editor.CreateView(isReadOnly: false);
        _textView = textView;

        textView.Caret.PositionChanged += CaretOnPositionChanged;
        buffer.Changed += (o, e) => viewModel.OnTextChanged();

        viewModel.Initialize(documentId, OnError,
            () => GetSelectionSpan(),
            this);
    }

    /// <summary>
    /// Writes workspace-applied changes (code fixes, formatting, rename) back into the editor
    /// buffer as minimal edits; the open-document tracking round-trips the edit into the
    /// Roslyn solution.
    /// </summary>
    private void OnTextUpdated(SourceText text)
    {
        if (_buffer is not { } buffer)
        {
            return;
        }

        var oldText = buffer.CurrentSnapshot.AsText();
        using var edit = buffer.CreateEdit();
        foreach (var change in text.GetTextChanges(oldText))
        {
            edit.Replace(new Span(change.Span.Start, change.Span.Length), change.NewText);
        }

        edit.Apply();
    }

    private TextSpan GetSelectionSpan()
    {
        if (_textView is not { } textView)
        {
            return default;
        }

        var span = textView.Selection.StreamSelectionSpan.SnapshotSpan;
        return new TextSpan(span.Start.Position, span.Length);
    }

    private void FocusEditor() => _editor.FocusEditor();

    private FindReplacePanel? FindReplace => _textView is { } textView ? FindReplacePanel.Get(textView) : null;

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
        this.AddKeyBinding(KeyBindingCommands.Find, new DelegateCommand(() => FindReplace?.Show(showReplace: false)));
        this.AddKeyBinding(KeyBindingCommands.Replace, new DelegateCommand(() => FindReplace?.Show(showReplace: true)));
        this.AddKeyBinding(KeyBindingCommands.FindNext, new DelegateCommand(() => FindReplace?.FindNext()));
        this.AddKeyBinding(KeyBindingCommands.FindPrevious, new DelegateCommand(() => FindReplace?.FindPrevious()));
        this.AddKeyBinding(KeyBindingCommands.SearchReplaceNext, new DelegateCommand(() => FindReplace?.ReplaceNext()));
        this.AddKeyBinding(KeyBindingCommands.SearchReplaceAll, new DelegateCommand(() => FindReplace?.ReplaceAll()));
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
            FocusEditor();
        }
    }

    private async void OnReadInput()
    {
        var textBox = new TextBox();

        var dialog = new HeaderedContentControl
        {
            Header = "Console Input",
            Content = textBox,
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

    private void NuGetOnPackageInstalled(PackageData package)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_buffer is not { } buffer)
            {
                return;
            }

            var text = ViewModel.FormatPackageReference(package.Id, package.Version);
            buffer.Insert(0, text);
        });
    }

    private void OnError(ExceptionResultObject? e)
    {
    }

    public void Dispose()
    {
        if (_textView is { } textView)
        {
            textView.Caret.PositionChanged -= CaretOnPositionChanged;
            _textView = null;
        }

        _editor.Dispose();
    }
}
