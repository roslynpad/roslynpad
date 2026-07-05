namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class LineStartCommandArgs : EditorCommandArgs
    {
        public LineStartCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
