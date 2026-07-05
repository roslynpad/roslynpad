namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class DuplicateSelectionCommandArgs: EditorCommandArgs
    {
        public DuplicateSelectionCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
