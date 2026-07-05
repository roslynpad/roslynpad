namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class UncollapseTagCommandArgs : EditorCommandArgs
    {
        public UncollapseTagCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
