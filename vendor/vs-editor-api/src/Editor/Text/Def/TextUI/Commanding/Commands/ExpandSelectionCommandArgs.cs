namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ExpandSelectionCommandArgs: EditorCommandArgs
    {
        public ExpandSelectionCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
