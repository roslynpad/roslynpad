using System.Threading.Tasks;

namespace RoslynPad.UI;

public interface IOpenFileDialog
{
    bool AllowMultiple { get; set; }

    FileDialogFilter Filter { set; }

    string InitialDirectory { get; set; }

    string FileName { get; set; }

    Task<string[]?> ShowAsync();
}
