namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ViewCallHierarchyCommandArgs : EditorCommandArgs
    {
        public ViewCallHierarchyCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
