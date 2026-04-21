namespace RoslynPad.UI;

public interface IRenameDocumentDialog : IDialog
{
    string? DocumentName { get; set; }
    bool ShouldRename { get; }
    void Initialize(string documentName);
}
