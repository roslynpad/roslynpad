namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class GoToDefinitionCommandArgs : EditorCommandArgs
    {
        public GoToDefinitionCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
