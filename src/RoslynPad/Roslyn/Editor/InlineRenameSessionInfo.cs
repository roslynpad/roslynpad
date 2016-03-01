using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.Editor
{
    public sealed class InlineRenameSessionInfo
    {
        public bool CanRename { get; }

        public string LocalizedErrorMessage { get; }

        public IInlineRenameSession Session { get; }

        public InlineRenameSessionInfo(object inner)
        {
            CanRename = inner.GetPropertyValue<bool>(nameof(CanRename));
            LocalizedErrorMessage = inner.GetPropertyValue<string>(nameof(LocalizedErrorMessage));
            Session = new InlineRenameSession(inner.GetPropertyValue<object>(nameof(Session)));
        }
    }
}