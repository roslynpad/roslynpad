using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Search;

namespace RoslynPad;

partial class ILViewer : UserControl
{
    static ILViewer()
    {
        TextProperty.Changed.AddClassHandler<ILViewer>(OnTextChanged);
        EditorFontFamilyProperty.Changed.AddClassHandler<ILViewer>(OnEditorFontFamilyChanged);

        HighlightingManager.Instance.RegisterHighlighting(
            "ILAsm", [".il"],
            () =>
            {
                using var stream = typeof(ILViewer).Assembly.GetManifestResourceStream("RoslynPad.ILAsm-Mode.xshd")!;
                using var reader = new XmlTextReader(stream);
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
            });
    }

    public ILViewer()
    {
        InitializeComponent();

        var editor = this.FindControl<TextEditor>("TextEditor")!;
        editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("ILAsm");
        editor.Document.FileName = "dasm.il";
        SearchPanel.Install(editor);
    }

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ILViewer, string?>(nameof(Text));

    public static readonly StyledProperty<string?> EditorFontFamilyProperty =
        AvaloniaProperty.Register<ILViewer, string?>(nameof(EditorFontFamily));

    private static void OnTextChanged(ILViewer viewer, AvaloniaPropertyChangedEventArgs e)
    {
        var editor = viewer.FindControl<TextEditor>("TextEditor")!;
        editor.Document.Text = e.NewValue as string ?? string.Empty;
    }

    private static void OnEditorFontFamilyChanged(ILViewer viewer, AvaloniaPropertyChangedEventArgs e)
    {
        var editor = viewer.FindControl<TextEditor>("TextEditor")!;
        var fonts = (e.NewValue as string)?.Split(',') ?? [];
        foreach (var font in fonts)
        {
            try
            {
                editor.FontFamily = FontFamily.Parse(font);
                return;
            }
            catch
            {
            }
        }
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? EditorFontFamily
    {
        get => GetValue(EditorFontFamilyProperty);
        set => SetValue(EditorFontFamilyProperty, value);
    }
}
