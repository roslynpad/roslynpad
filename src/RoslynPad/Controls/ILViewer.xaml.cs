using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;

namespace RoslynPad.Controls;

public partial class ILViewer
{
    static ILViewer()
    {
        HighlightingManager.Instance.RegisterHighlighting(
            "ILAsm", [".il"],
            () =>
            {
                using var stream = typeof(ILViewer).Assembly.GetManifestResourceStream(typeof(ILViewer), "ILAsm-Mode.xshd")!;
                using var reader = new XmlTextReader(stream);
                return HighlightingLoader.Load(reader, HighlightingManager.Instance);
            });
    }

    public ILViewer()
    {
        InitializeComponent();

        TextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("ILAsm");
        TextEditor.Document.FileName = "dasm.il";
        SearchPanel.Install(TextEditor);
        TextEditor.ContextMenu = new ContextMenu
        {
            Items =
            {
                new MenuItem { Command = ApplicationCommands.Copy }
            }
        };
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        "Text", typeof(string), typeof(ILViewer), new FrameworkPropertyMetadata(OnTextChanged));

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ILViewer)d).TextEditor.Document.Text = e.NewValue as string ?? string.Empty;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
