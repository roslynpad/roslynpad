using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using DialogHostAvalonia;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using RoslynPad.Build;
using RoslynPad.Roslyn.FileBasedPrograms;
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
        viewModel.RenameRequested += (o, e) => _editor.InvokeRename();
        viewModel.NavigationRequested += span => _editor.NavigateToSpan(span);
        viewModel.FindRequested += (o, e) => _editor.InvokeFindReplace(showReplace: false);
        viewModel.FindReplaceRequested += (o, e) => _editor.InvokeFindReplace(showReplace: true);

        var documentText = await viewModel.LoadTextAsync().ConfigureAwait(true);

        var roslynHost = viewModel.MainViewModel.RoslynHost;

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

        await MigrateReferenceDirectivesAsync(viewModel, buffer, documentId).ConfigureAwait(true);
    }

    /// <summary>
    /// Rewrites legacy <c>#r</c> directives - illegal in a regular C# file - into their file-based
    /// app equivalents. Applied as a buffer edit so it joins the undo stack and leaves the document
    /// dirty; the user decides whether to keep it by saving.
    /// </summary>
    private async Task MigrateReferenceDirectivesAsync(OpenDocumentViewModel viewModel, ITextBuffer buffer, DocumentId documentId)
    {
        if (!viewModel.MainViewModel.Settings.MigrateReferenceDirectives ||
            viewModel.SourceCodeKind != SourceCodeKind.Regular ||
            viewModel.MainViewModel.RoslynHost.GetDocument(documentId) is not { } document)
        {
            return;
        }

        var root = await document.GetSyntaxRootAsync().ConfigureAwait(true);
        if (root is null)
        {
            return;
        }

        var changes = ReferenceDirectiveHelpers.GetMigrationChanges(root, await document.GetTextAsync().ConfigureAwait(true));
        if (changes.IsEmpty)
        {
            return;
        }

        using (var edit = buffer.CreateEdit())
        {
            foreach (var change in changes)
            {
                edit.Replace(new Span(change.Span.Start, change.Span.Length), change.NewText);
            }

            edit.Apply();
        }

        await ShowMigrationNoticeAsync(viewModel.MainViewModel.Settings).ConfigureAwait(true);
    }

    /// <summary>Explains the rewrite; dismissed by clicking anywhere outside it.</summary>
    private static async Task ShowMigrationNoticeAsync(IApplicationSettingsValues settings)
    {
        var content = new StackPanel
        {
            MaxWidth = 380,
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "This file used #r directives, which aren't valid C# outside scripts. " +
                        "They were rewritten as file-based app directives. Save the file to keep the change.",
                },
                new CheckBox
                {
                    Content = "Migrate #r directives when opening a file",
                    [!ToggleButton.IsCheckedProperty] =
                        new Binding(nameof(settings.MigrateReferenceDirectives)) { Source = settings },
                },
            },
        };

        await DialogHost.Show(
            new HeaderedContentControl { Header = "References updated", Content = content },
            MainWindow.DialogHostIdentifier).ConfigureAwait(true);
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
            FocusEditor();
        }
    }

    private async void OnReadInput()
    {
        var textBox = new TextBox
        {
            Width = 400,
            Height = 100,
            TextWrapping = TextWrapping.Wrap,
        };
        ScrollViewer.SetVerticalScrollBarVisibility(textBox, ScrollBarVisibility.Auto);

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
