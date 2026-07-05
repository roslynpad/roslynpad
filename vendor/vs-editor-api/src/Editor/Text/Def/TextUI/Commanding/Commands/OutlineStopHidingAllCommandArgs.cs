namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class OutlineStopHidingAllCommandArgs : EditorCommandArgs
    {
        public OutlineStopHidingAllCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
