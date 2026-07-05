namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class SelectContainingDeclarationCommandArgs : EditorCommandArgs
    {
        public SelectContainingDeclarationCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
