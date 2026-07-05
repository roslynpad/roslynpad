namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class LineEndExtendCommandArgs : EditorCommandArgs
    {
        public LineEndExtendCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
