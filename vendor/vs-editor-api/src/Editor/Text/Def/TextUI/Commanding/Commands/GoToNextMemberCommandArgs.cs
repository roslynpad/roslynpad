namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class GoToNextMemberCommandArgs : EditorCommandArgs
    {
        public GoToNextMemberCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
