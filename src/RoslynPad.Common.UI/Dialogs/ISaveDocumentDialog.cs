using System;

namespace RoslynPad.UI
{
    public interface ISaveDocumentDialog : IDialog
    {
        string DocumentName { get; set; }
        SaveResult Result { get; }
        bool AllowNameEdit { get; set; }
        bool ShowDontSave { get; set; }
        string FilePath { get; }
        Func<string, string> FilePathFactory { get; set; }
    }

    public enum SaveResult
    {
        Cancel,
        Save,
        DontSave
    }
}