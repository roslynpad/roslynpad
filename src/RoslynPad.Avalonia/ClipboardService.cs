using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace RoslynPad;

[Export(typeof(UI.IClipboardService))]
internal class ClipboardService : UI.IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window?.Clipboard is { } clipboard)
        {
            await clipboard.SetTextAsync(text).ConfigureAwait(false);
        }
    }
}
