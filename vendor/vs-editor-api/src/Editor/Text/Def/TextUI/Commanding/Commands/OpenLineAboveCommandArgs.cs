namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class OpenLineAboveCommandArgs : EditorCommandArgs
    {
        public OpenLineAboveCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
