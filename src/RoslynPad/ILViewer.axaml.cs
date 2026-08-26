using Avalonia;
using Avalonia.Controls;
using Microsoft.VisualStudio.Text;
using RoslynPad.Editor;
using RoslynPad.UI;

namespace RoslynPad;

internal partial class ILViewer : UserControl
{
    private readonly CodeEditorView _editor;

    static ILViewer()
    {
        TextProperty.Changed.AddClassHandler<ILViewer>((viewer, e) => viewer.OnTextChanged(e.NewValue as string));
        ViewModelProperty.Changed.AddClassHandler<ILViewer>((viewer, e) => viewer.OnTextChanged(viewer.Text));
    }

    public ILViewer()
    {
        InitializeComponent();

        _editor = this.FindControl<CodeEditorView>("Editor") ?? throw new InvalidOperationException("Missing Editor");
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
        if (_editor.Buffer is { } buffer)
        {
            buffer.Replace(new Span(0, buffer.CurrentSnapshot.Length), text ?? string.Empty);
            return;
        }

        // The editor is created on the first IL text; by then the Roslyn host is initialized
        // since the IL comes from building an open document.
        if (!string.IsNullOrEmpty(text) && ViewModel is { IsInitialized: true } viewModel)
        {
            _editor.CreateBuffer(viewModel, text, ILClassificationDefinitions.ContentType);
            _editor.CreateView(isReadOnly: true, setFocus: false);
        }
    }
}
