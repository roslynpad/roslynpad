namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class GoToContainingDeclarationCommandArgs : EditorCommandArgs
    {
        public GoToContainingDeclarationCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
