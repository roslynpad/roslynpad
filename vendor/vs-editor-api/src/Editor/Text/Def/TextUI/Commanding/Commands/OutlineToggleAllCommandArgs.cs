namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class OutlineToggleAllCommandArgs : EditorCommandArgs
    {
        public OutlineToggleAllCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
