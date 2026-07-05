namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class CommitUniqueCompletionListItemCommandArgs : EditorCommandArgs
    {
        public CommitUniqueCompletionListItemCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
