namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class MoveSelectedLinesDownCommandArgs : EditorCommandArgs
    {
        public MoveSelectedLinesDownCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
