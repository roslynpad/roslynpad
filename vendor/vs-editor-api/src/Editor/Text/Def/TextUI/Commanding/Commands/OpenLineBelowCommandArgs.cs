namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class OpenLineBelowCommandArgs : EditorCommandArgs
    {
        public OpenLineBelowCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
