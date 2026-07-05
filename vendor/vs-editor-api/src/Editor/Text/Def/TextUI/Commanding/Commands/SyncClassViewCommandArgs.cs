namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class SyncClassViewCommandArgs : EditorCommandArgs
    {
        public SyncClassViewCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
