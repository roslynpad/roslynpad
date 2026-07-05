namespace Microsoft.VisualStudio.Text.Editor.Commanding
{
    /// <summary>
    /// A dispatcher to execute commands on a text view based on standard macOS selectors.
    /// </summary>
    public interface ISelectorCommandDispatcher
    {
        bool RespondsToSelector(string selector, object sender, int? tag = null);
        void DoCommandBySelector(string selector, object sender, int? tag = null);
        void ReplaceText(Span replaceSpan, string text);
        void InsertChar(char c);
        void InsertChars(string chars);
    }
}