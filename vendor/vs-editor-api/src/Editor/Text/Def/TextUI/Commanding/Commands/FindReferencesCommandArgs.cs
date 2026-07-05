namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class FindReferencesCommandArgs : EditorCommandArgs
    {
        public FindReferencesCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
