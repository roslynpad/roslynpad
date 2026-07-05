namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class GotoBraceExtCommandArgs : EditorCommandArgs
    {
        public GotoBraceExtCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }

}
