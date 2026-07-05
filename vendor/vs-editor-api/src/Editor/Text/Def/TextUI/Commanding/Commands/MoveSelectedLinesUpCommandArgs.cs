namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class MoveSelectedLinesUpCommandArgs : EditorCommandArgs
    {
        public MoveSelectedLinesUpCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
