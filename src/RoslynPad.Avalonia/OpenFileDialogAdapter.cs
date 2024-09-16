using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using RoslynPad.UI;

namespace RoslynPad;

[Export(typeof(IOpenFileDialog))]
internal class OpenFileDialogAdapter : IOpenFileDialog
{
    public bool AllowMultiple { get; set; }

    public FileDialogFilter? Filter { get; set; }

    public string InitialDirectory { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public async Task<string[]?> ShowAsync()
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.FirstOrDefault(w => w.IsActive);

        if (window == null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            AllowMultiple = AllowMultiple,
            SuggestedStartLocation = await window.StorageProvider.TryGetFolderFromPathAsync(InitialDirectory).ConfigureAwait(false),
        };

        if (Filter != null)
        {
            options.FileTypeFilter =
            [
                new FilePickerFileType(Filter.Header) { Patterns = Filter.Extensions.AsReadOnly() }
            ];
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(options).ConfigureAwait(false);

        return files.Select(file => file.Path.ToString()).ToArray();
    }
}
