namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class GoToPreviousMemberCommandArgs : EditorCommandArgs
    {
        public GoToPreviousMemberCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
