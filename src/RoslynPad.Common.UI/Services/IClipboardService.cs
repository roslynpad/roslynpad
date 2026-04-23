namespace RoslynPad.UI;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
