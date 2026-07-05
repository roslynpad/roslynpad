namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class EncapsulateFieldCommandArgs : EditorCommandArgs
    {
        public EncapsulateFieldCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
