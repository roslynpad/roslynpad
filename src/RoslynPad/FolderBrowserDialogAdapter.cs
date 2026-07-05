using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using RoslynPad.UI;

namespace RoslynPad;

[Export(typeof(IFolderBrowserDialog))]
internal class FolderBrowserDialogAdapter : IFolderBrowserDialog
{
    public bool ShowEditBox { get; set; }

    public string SelectedPath { get; set; } = string.Empty;

    public async Task<bool?> ShowAsync()
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?
            .Windows.FirstOrDefault(w => w.IsActive);

        if (window == null)
        {
            return false;
        }

        var options = new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(SelectedPath).ConfigureAwait(false),
        };

        var folders = await window.StorageProvider.OpenFolderPickerAsync(options).ConfigureAwait(false);

        if (folders.Count > 0)
        {
            SelectedPath = folders[0].Path.LocalPath;
            return true;
        }

        return false;
    }
}
