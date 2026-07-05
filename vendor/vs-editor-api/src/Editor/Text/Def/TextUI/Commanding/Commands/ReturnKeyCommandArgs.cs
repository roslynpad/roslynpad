namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ReturnKeyCommandArgs : EditorCommandArgs
    {
        public ReturnKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
