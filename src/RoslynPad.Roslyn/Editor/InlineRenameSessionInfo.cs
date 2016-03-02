namespace RoslynPad.Roslyn.Editor
{
    public sealed class InlineRenameSessionInfo
    {
        public bool CanRename { get; }

        public string LocalizedErrorMessage { get; }

        public IInlineRenameSession Session { get; }

        internal InlineRenameSessionInfo(Microsoft.CodeAnalysis.Editor.InlineRenameSessionInfo inner)
        {
            CanRename = inner.CanRename;
            LocalizedErrorMessage = inner.LocalizedErrorMessage;
            Session = new InlineRenameSession(inner.Session);
        }
    }
}