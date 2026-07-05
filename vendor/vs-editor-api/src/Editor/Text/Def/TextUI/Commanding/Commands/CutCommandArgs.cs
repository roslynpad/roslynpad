namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class CutCommandArgs : EditorCommandArgs
    {
        public CutCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
