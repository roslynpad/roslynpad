namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class InvokeQuickInfoCommandArgs : EditorCommandArgs
    {
        public InvokeQuickInfoCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
