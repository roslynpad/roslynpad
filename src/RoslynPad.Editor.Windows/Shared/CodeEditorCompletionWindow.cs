namespace RoslynPad.Editor;

public partial class CodeEditorCompletionWindow : CompletionWindow
{
    private bool _isSoftSelectionActive;
    private KeyEventArgs? _keyDownArgs;

    public CodeEditorCompletionWindow(TextArea textArea) : base(textArea)
    {
        _isSoftSelectionActive = true;
        CompletionList.SelectionChanged += CompletionListOnSelectionChanged;
        Loaded += (_,_) => Initialize();
    }

    partial void Initialize();

    private void CompletionListOnSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (!UseHardSelection &&
            _isSoftSelectionActive && _keyDownArgs?.Handled != true
            && args.AddedItems?.Count > 0)
        {
            CompletionList.SelectedItem = null;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Home || e.Key == Key.End) return;

        _keyDownArgs = e;

        base.OnKeyDown(e);

        SetSoftSelection(e);
    }

    private void SetSoftSelection(RoutedEventArgs e)
    {
        if (e.Handled)
        {
            _isSoftSelectionActive = false;
        }
    }

    public bool UseHardSelection { get; set; }
}
