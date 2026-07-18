using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using RoslynPad.Editor;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Hosts a Morgania text view with the app's font and theme wiring — the editor-hosting core
/// shared by <see cref="DocumentView"/> (writable) and <see cref="MetadataDocumentView"/>
/// (read-only). Two-phase: <see cref="CreateBuffer"/> first, so the caller can open the Roslyn
/// document over the buffer before <see cref="CreateView"/> spins up the taggers.
/// </summary>
internal sealed class CodeEditorView : ContentControl, IDisposable
{
    private MainViewModel? _mainViewModel;
    private IClassificationFormatMap? _formatMap;
    private IClassificationTypeRegistryService? _classificationRegistry;
    private IEditorFormatMap? _editorFormatMap;

    public ITextBuffer? Buffer { get; private set; }
    public IWpfTextView? TextView { get; private set; }

    public ITextBuffer CreateBuffer(MainViewModel mainViewModel, string text)
    {
        _mainViewModel = mainViewModel;
        var exportProvider = mainViewModel.RoslynHost.ExportProvider;

        var contentType = exportProvider.GetExportedValue<IContentTypeRegistryService>().GetContentType("CSharp")
            ?? throw new InvalidOperationException("The CSharp content type is not registered");
        Buffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(text, contentType);
        return Buffer;
    }

    public IWpfTextView CreateView(bool isReadOnly)
    {
        var mainViewModel = _mainViewModel ?? throw new InvalidOperationException($"{nameof(CreateBuffer)} was not called");
        var exportProvider = mainViewModel.RoslynHost.ExportProvider;

        var editorFactory = exportProvider.GetExportedValue<ITextEditorFactoryService>();
        var textView = editorFactory.CreateTextView(Buffer!);
        TextView = textView;
        textView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
        if (isReadOnly)
        {
            textView.Options.SetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId, true);
        }
        else
        {
            textView.Options.SetOptionValue(DefaultTextViewOptions.BraceCompletionEnabledOptionId, true);
        }

        _formatMap = exportProvider.GetExportedValue<IClassificationFormatMapService>().GetClassificationFormatMap(textView);
        _classificationRegistry = exportProvider.GetExportedValue<IClassificationTypeRegistryService>();
        _editorFormatMap = exportProvider.GetExportedValue<IEditorFormatMapService>().GetEditorFormatMap(textView);

        ApplyFontSettings(mainViewModel.Settings.EditorFontFamily, mainViewModel.EditorFontSize);
        ApplyTheme();
        mainViewModel.EditorFontSizeChanged += OnEditorFontSizeChanged;
        mainViewModel.ThemeChanged += OnThemeChanged;

        Content = editorFactory.CreateTextViewHost(textView, setFocus: true).HostControl;
        return textView;
    }

    /// <summary>Selects the span, scrolls it into view, and focuses the editor.</summary>
    public void NavigateToSpan(TextSpan span)
    {
        if (TextView is not { } textView)
        {
            return;
        }

        // Before the first arrange the viewport is 0x0, so EnsureSpanVisible would pin the span
        // to the top-left corner instead of centering it; wait for the view to get its real size.
        if (textView.ViewportHeight == 0)
        {
            textView.ViewportHeightChanged += NavigateWhenSized;
            return;

            void NavigateWhenSized(object? sender, EventArgs e)
            {
                textView.ViewportHeightChanged -= NavigateWhenSized;
                NavigateToSpan(span);
            }
        }

        var snapshot = textView.TextSnapshot;
        var snapshotSpan = new SnapshotSpan(snapshot, Span.FromBounds(
            Math.Min(span.Start, snapshot.Length), Math.Min(span.End, snapshot.Length)));

        textView.Selection.Select(snapshotSpan, isReversed: false);
        textView.Caret.MoveTo(snapshotSpan.End);
        textView.ViewScroller.EnsureSpanVisible(snapshotSpan, EnsureSpanVisibleOptions.AlwaysCenter);
        FocusEditor();
    }

    public void FocusEditor() => TextView?.VisualElement.Focus();

    public void ApplyFontSettings(string fontFamilies, double fontSize)
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

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTheme();

    private void ApplyTheme()
    {
        if (_mainViewModel is not { } mainViewModel || _formatMap is not { } formatMap ||
            _classificationRegistry is not { } registry || TextView is not { } textView)
        {
            return;
        }

        var theme = new ThemeClassificationFormats(mainViewModel.Theme);
        theme.Apply(formatMap, registry);

        if (_editorFormatMap is { } editorFormatMap)
        {
            theme.ApplyPopup(editorFormatMap);
            theme.ApplyBraceMatching(editorFormatMap);
            theme.ApplyReferenceHighlighting(editorFormatMap);
            theme.ApplyCaret(editorFormatMap);
            theme.ApplyFindReplace(editorFormatMap);
            theme.ApplyBackgroundWorkIndicator(editorFormatMap);
        }

        // Glyph drawings (completion icons, quick info symbols, the light bulb) adapt their
        // colors to the theme's editor background.
        Morgania.CodeAnalysis.Editor.ImageCatalog.ThemeBackground = theme.Background;

        if (theme.Background is { } background)
        {
            textView.Background = new SolidColorBrush(background);
        }
    }

    public void Dispose()
    {
        if (_mainViewModel is { } mainViewModel)
        {
            mainViewModel.EditorFontSizeChanged -= OnEditorFontSizeChanged;
            mainViewModel.ThemeChanged -= OnThemeChanged;
        }

        if (TextView is { } textView)
        {
            textView.Close();
            TextView = null;
        }
    }
}
