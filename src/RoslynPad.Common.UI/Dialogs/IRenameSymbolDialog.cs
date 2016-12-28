namespace RoslynPad.UI
{
    public interface IRenameSymbolDialog : IDialog
    {
        bool ShouldRename { get; }
        string SymbolName { get; set; }
        void Initialize(string symbolName);
    }
}