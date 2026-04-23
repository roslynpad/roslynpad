namespace RoslynPad;

[Export(typeof(UI.IClipboardService))]
internal class ClipboardService : UI.IClipboardService
{
    public Task SetTextAsync(string text)
    {
        Clipboard.SetText(text);
        return Task.CompletedTask;
    }
}
