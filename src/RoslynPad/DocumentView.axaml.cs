using System.Globalization;
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
    private readonly TextBlock _lnTextBlock;
    private readonly TextBlock _colTextBlock;
    private readonly ContentControl _editorHost;

    private IWpfTextView? _textView;
    private ITextBuffer? _buffer;
    private IClassificationFormatMap? _formatMap;
    private IClassificationTypeRegistryService? _classificationRegistry;
    private IEditorFormatMap? _editorFormatMap;

    public DocumentView()
    {
        InitializeComponent();

        _editorHost = this.FindControl<ContentControl>("EditorHost") ?? throw new InvalidOperationException("Missing EditorHost");
        _nuGetSearch = this.FindControl<TextBox>("NuGetSearch") ?? throw new InvalidOperationException("Missing NuGetSearch");
        _lnTextBlock = this.FindControl<TextBlock>("Ln") ?? throw new InvalidOperationException("Missing Ln");
        _colTextBlock = this.FindControl<TextBlock>("Col") ?? throw new InvalidOperationException("Missing Col");

        _nuGetSearch.KeyDown += NuGetSearch_OnKeyDown;

        DataContextChanged += OnDataContextChanged;
    }

    public OpenDocumentViewModel ViewModel { get => field.NotNull(); private set; }

    private void CaretOnPositionChanged(object? sender, CaretPositionChangedEventArgs e)
    {
        var position = e.NewPosition.BufferPosition;
        var line = position.GetContainingLine();
        _lnTextBlock.Text = (line.LineNumber + 1).ToString(CultureInfo.InvariantCulture);
        _colTextBlock.Text = (position.Position - line.Start.Position + 1).ToString(CultureInfo.InvariantCulture);
    }

    private async void OnDataContextChanged(object? sender, EventArgs args)
    {
        if (DataContext is not OpenDocumentViewModel viewModel) return;
        ViewModel = viewModel;

        InitializeKeyBindings(viewModel);

        viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;

        viewModel.ReadInput += OnReadInput;
        viewModel.EditorFocus += (o, e) => FocusEditor();
        viewModel.FindRequested += (o, e) => FindReplace?.Show(showReplace: false);
        viewModel.FindReplaceRequested += (o, e) => FindReplace?.Show(showReplace: true);
        viewModel.MainViewModel.EditorFontSizeChanged += OnEditorFontSizeChanged;
        viewModel.MainViewModel.ThemeChanged += OnThemeChanged;

        var documentText = await viewModel.LoadTextAsync().ConfigureAwait(true);

        var roslynHost = viewModel.MainViewModel.RoslynHost;
        var exportProvider = roslynHost.ExportProvider;

        Morgania.CodeAnalysis.Editor.DiagnosticsSquiggles.DisabledDiagnostics = roslynHost.DisabledDiagnostics;

        var contentType = exportProvider.GetExportedValue<IContentTypeRegistryService>().GetContentType("CSharp")
            ?? throw new InvalidOperationException("The CSharp content type is not registered");
        var buffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(documentText, contentType);
        _buffer = buffer;

        var documentId = roslynHost.AddDocument(new RoslynPad.Roslyn.DocumentCreationArgs(
            buffer.AsTextContainer(),
            viewModel.WorkingDirectory,
            viewModel.SourceCodeKind,
            OnTextUpdated,
            viewModel.Document?.Name));

        var editorFactory = exportProvider.GetExportedValue<ITextEditorFactoryService>();
        var textView = editorFactory.CreateTextView(buffer);
        _textView = textView;
        textView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
        textView.Options.SetOptionValue(DefaultTextViewOptions.BraceCompletionEnabledOptionId, true);

        _formatMap = exportProvider.GetExportedValue<IClassificationFormatMapService>().GetClassificationFormatMap(textView);
        _classificationRegistry = exportProvider.GetExportedValue<IClassificationTypeRegistryService>();
        _editorFormatMap = exportProvider.GetExportedValue<IEditorFormatMapService>().GetEditorFormatMap(textView);

        ApplyFontSettings(viewModel.MainViewModel.Settings.EditorFontFamily, viewModel.MainViewModel.EditorFontSize);
        ApplyTheme();

        var viewHost = editorFactory.CreateTextViewHost(textView, setFocus: true);
        _editorHost.Content = viewHost.HostControl;

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

    private void FocusEditor() => _textView?.VisualElement.Focus();

    private FindReplacePanel? FindReplace => _textView is { } textView ? FindReplacePanel.Get(textView) : null;

    private void ApplyFontSettings(string fontFamilies, double fontSize)
    {
        if (_formatMap is not { } formatMap)
        {
            return;
        }

        var properties = formatMap.DefaultTextProperties.SetFontRenderingEmSize(fontSize);

        foreach (var font in fontFamilies.Split(','))
        {
            try
            {
                properties = properties.SetTypeface(new Typeface(FontFamily.Parse(font.Trim())));
                break;
            }
            catch
            {
            }
        }

        formatMap.DefaultTextProperties = properties;
    }

    private void OnEditorFontSizeChanged(double fontSize)
    {
        if (_formatMap is { } formatMap)
        {
            formatMap.DefaultTextProperties = formatMap.DefaultTextProperties.SetFontRenderingEmSize(fontSize);
        }
    }

    private void ApplyTheme()
    {
        if (_formatMap is not { } formatMap || _classificationRegistry is not { } registry || _textView is not { } textView)
        {
            return;
        }

        var theme = new ThemeClassificationFormats(ViewModel.MainViewModel.Theme);
        theme.Apply(formatMap, registry);

        if (_editorFormatMap is { } editorFormatMap)
        {
            theme.ApplyPopup(editorFormatMap);
            theme.ApplyBraceMatching(editorFormatMap);
            theme.ApplyCaret(editorFormatMap);
            theme.ApplyFindReplace(editorFormatMap);
        }

        // Glyph drawings (completion icons, quick info symbols, the light bulb) adapt their
        // colors to the theme's editor background.
        Morgania.CodeAnalysis.Editor.ImageCatalog.ThemeBackground = theme.Background;

        if (theme.Background is { } background)
        {
            textView.Background = new SolidColorBrush(background);
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();

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
            textView.Close();
            _textView = null;
        }
    }
}
