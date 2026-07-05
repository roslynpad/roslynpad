namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class InvokeQuickFixCommandArgs : EditorCommandArgs
    {
        public InvokeQuickFixCommandArgs (ITextView textView, ITextBuffer subjectBuffer) : base (textView, subjectBuffer)
        {
        }
    }
}
