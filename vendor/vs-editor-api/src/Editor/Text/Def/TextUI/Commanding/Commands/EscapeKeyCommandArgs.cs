namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class EscapeKeyCommandArgs : EditorCommandArgs
    {
        public EscapeKeyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
