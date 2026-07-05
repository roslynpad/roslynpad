namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class InvokeSignatureHelpCommandArgs : EditorCommandArgs
    {
        public InvokeSignatureHelpCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
