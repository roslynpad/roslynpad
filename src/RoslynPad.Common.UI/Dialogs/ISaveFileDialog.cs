using System.Threading.Tasks;

namespace RoslynPad.UI
{
    public interface ISaveFileDialog
    {
        bool OverwritePrompt { get; set; }
        bool AddExtension { get; set; }
        FileDialogFilter Filter { set; }
        string DefaultExt { get; set; }
        string FileName { get; set; }

        Task<string?> ShowAsync();
    }
}