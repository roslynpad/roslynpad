using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using RoslynPad.Editor;
using RoslynPad.UI;

namespace RoslynPad;

partial class ILViewer : UserControl
{
    private readonly ContentControl _editorHost;

    private ITextBuffer? _buffer;
    private IWpfTextView? _textView;

    static ILViewer()
    {
        TextProperty.Changed.AddClassHandler<ILViewer>((viewer, e) => viewer.OnTextChanged(e.NewValue as string));
        ViewModelProperty.Changed.AddClassHandler<ILViewer>((viewer, e) => viewer.OnTextChanged(viewer.Text));
    }

    public ILViewer()
    {
        InitializeComponent();

        _editorHost = this.FindControl<ContentControl>("EditorHost") ?? throw new InvalidOperationException("Missing EditorHost");
    }

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ILViewer, string?>(nameof(Text));

    public static readonly StyledProperty<MainViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<ILViewer, MainViewModel?>(nameof(ViewModel));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MainViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private void OnTextChanged(string? text)
    {
        if (_buffer is { } buffer)
        {
            buffer.Replace(new Span(0, buffer.CurrentSnapshot.Length), text ?? string.Empty);
            return;
        }

        // The editor is created on the first IL text; by then the Roslyn host is initialized
        // since the IL comes from building an open document.
        if (!string.IsNullOrEmpty(text) && ViewModel is { IsInitialized: true } viewModel)
        {
            CreateEditor(viewModel, text);
        }
    }

    private void CreateEditor(MainViewModel viewModel, string text)
    {
        var exportProvider = viewModel.RoslynHost.ExportProvider;

        var contentType = exportProvider.GetExportedValue<IContentTypeRegistryService>().GetContentType(ILClassificationDefinitions.ContentType)
            ?? throw new InvalidOperationException("The ILAsm content type is not registered");
        var buffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(text, contentType);
        _buffer = buffer;

        var editorFactory = exportProvider.GetExportedValue<ITextEditorFactoryService>();
        var textView = editorFactory.CreateTextView(buffer);
        _textView = textView;
        textView.Options.SetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId, true);

        ApplyTheme(viewModel);
        viewModel.ThemeChanged += (o, e) => ApplyTheme(viewModel);

        var viewHost = editorFactory.CreateTextViewHost(textView, setFocus: false);
        _editorHost.Content = viewHost.HostControl;
    }

    private void ApplyTheme(MainViewModel viewModel)
    {
        if (_textView is { } textView && viewModel.Theme.TryGetColor("editor.background") is { } background)
        {
            textView.Background = new SolidColorBrush(Color.Parse(background));
        }
    }
}
