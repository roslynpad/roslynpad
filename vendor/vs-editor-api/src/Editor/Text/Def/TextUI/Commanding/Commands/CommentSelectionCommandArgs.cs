namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class CommentSelectionCommandArgs : EditorCommandArgs
    {
        public CommentSelectionCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
