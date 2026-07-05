namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class LineEndCommandArgs : EditorCommandArgs
    {
        public LineEndCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
