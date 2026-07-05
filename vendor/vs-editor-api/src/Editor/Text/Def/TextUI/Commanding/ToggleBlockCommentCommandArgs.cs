namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ToggleBlockCommentCommandArgs : EditorCommandArgs
    {
        public ToggleBlockCommentCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
