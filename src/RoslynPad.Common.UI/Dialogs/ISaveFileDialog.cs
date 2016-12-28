namespace RoslynPad.UI
{
    public interface ISaveFileDialog
    {
        bool OverwritePrompt { get; set; }
        bool AddExtension { get; set; }
        string Filter { get; set; }
        string DefaultExt { get; set; }
        string FileName { get; set; }

        bool? Show();
    }
}