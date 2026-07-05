namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class TypeCharCommandArgs : EditorCommandArgs
    {
        public char TypedChar { get; }
        public TypeCharCommandArgs(ITextView textView, ITextBuffer subjectBuffer, char typedChar) : base(textView, subjectBuffer)
        {
            TypedChar = typedChar;
        }
    }
}
