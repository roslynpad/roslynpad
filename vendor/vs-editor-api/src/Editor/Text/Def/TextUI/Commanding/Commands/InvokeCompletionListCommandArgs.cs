namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class InvokeCompletionListCommandArgs : EditorCommandArgs
    {
        public InvokeCompletionListCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
