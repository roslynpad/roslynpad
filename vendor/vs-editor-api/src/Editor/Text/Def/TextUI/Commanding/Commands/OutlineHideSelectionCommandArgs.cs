namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class OutlineHideSelectionCommandArgs : EditorCommandArgs
    {
        public OutlineHideSelectionCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
