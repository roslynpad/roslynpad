namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class OutlineCollapseToDefinitionsCommandArgs : EditorCommandArgs
    {
        public OutlineCollapseToDefinitionsCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
