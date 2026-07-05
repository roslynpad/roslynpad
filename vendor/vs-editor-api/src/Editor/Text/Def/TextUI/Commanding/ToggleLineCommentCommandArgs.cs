namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ToggleLineCommentCommandArgs: EditorCommandArgs
    {
        public ToggleLineCommentCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
