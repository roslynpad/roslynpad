namespace RoslynPad.Editor;

partial class CodeEditorCompletionWindow
{
    partial void Initialize()
    {
        CompletionList.ListBox.BorderThickness = new Thickness(1);
        CompletionList.ListBox.PointerPressed += (o, e) => _isSoftSelectionActive = false;
    }
}
