namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class InsertCommentCommandArgs : EditorCommandArgs
    {
        public InsertCommentCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
