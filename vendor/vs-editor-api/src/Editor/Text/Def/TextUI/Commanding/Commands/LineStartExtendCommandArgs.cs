namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class LineStartExtendCommandArgs : EditorCommandArgs
    {
        public LineStartExtendCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
