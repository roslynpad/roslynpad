namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class SurroundWithCommandArgs : EditorCommandArgs
    {
        public SurroundWithCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
