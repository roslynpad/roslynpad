namespace RoslynPad.UI;

public interface IFolderBrowserDialog
{
    bool ShowEditBox { get; set; }
    string SelectedPath { get; set; }
    Task<bool?> ShowAsync();
}