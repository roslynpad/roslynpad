using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using RoslynPad.UI;

namespace RoslynPad;

[Export(typeof(IOpenFileDialog))]
internal class OpenFileDialogAdapter : IOpenFileDialog
{
    public bool AllowMultiple { get; set; }

    public UI.FileDialogFilter? Filter { get; set; }

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
            SuggestedStartLocation = new BclStorageFolder(InitialDirectory),
        };

        if (Filter != null)
        {
            options.FileTypeFilter = new[]
            {
                new FilePickerFileType(Filter.Header) { Patterns = Filter.Extensions.AsReadOnly() }
            };
        }

        var file = await window.StorageProvider.OpenFilePickerAsync(options).ConfigureAwait(false);

        return file.Select(f => { f.TryGetUri(out var uri); return uri?.LocalPath!; })
            .Where(f => f != null).ToArray();
    }
}
