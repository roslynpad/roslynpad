namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class GotoBraceCommandArgs : EditorCommandArgs
    {
        public GotoBraceCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }

}
