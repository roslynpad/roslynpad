namespace RoslynPad.Roslyn
{
    public interface IEditorCaretProvider
    {
        int CaretPosition { get; }

        bool TryMoveCaret(int position);
    }
}