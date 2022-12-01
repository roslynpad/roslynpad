using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Platform.Storage;
using RoslynPad.UI;
using System;

namespace RoslynPad;

[Export(typeof(ISaveFileDialog))]
internal class SaveFileDialogAdapter : ISaveFileDialog
{
    public bool OverwritePrompt { get; set; }

    public bool AddExtension
    {
        get => false;
        set { }
    }

    public UI.FileDialogFilter? Filter { get; set; }

    public string DefaultExt { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public async Task<string?> ShowAsync()
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.FirstOrDefault(w => w.IsActive);

        if (window == null)
        {
            return null;
        }

        var options = new FilePickerSaveOptions
        {
            DefaultExtension = DefaultExt,
            ShowOverwritePrompt = OverwritePrompt,
            SuggestedFileName = FileName,
        };

        if (Filter != null)
        {
            options.FileTypeChoices = new[]
            {
                new FilePickerFileType(Filter.Header) { Patterns = Filter.Extensions.AsReadOnly() }
            };
        }

        var file = await window.StorageProvider.SaveFilePickerAsync(options).ConfigureAwait(false);
        return file?.TryGetUri(out var uri) == true ? uri.LocalPath : null;
    }
}
