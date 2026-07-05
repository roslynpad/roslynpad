namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class OutlineToggleCurrentCommandArgs : EditorCommandArgs
    {
        public OutlineToggleCurrentCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
