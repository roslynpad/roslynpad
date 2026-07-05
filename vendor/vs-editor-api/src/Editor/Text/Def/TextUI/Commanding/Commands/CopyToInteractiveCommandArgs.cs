namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class CopyToInteractiveCommandArgs : EditorCommandArgs
    {
        public CopyToInteractiveCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
