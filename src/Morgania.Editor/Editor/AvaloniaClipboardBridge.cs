#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Avalonia.Controls;
using Avalonia.Input.Platform;

using Microsoft.VisualStudio.Text.Operations.Implementation;

/// <summary>
/// Installs an Avalonia-backed provider into the editor-operations clipboard seam.
/// The in-process store stays authoritative (the shim's API is synchronous); text copied
/// in the editor is additionally pushed to the OS clipboard asynchronously. Reading the OS
/// clipboard for external paste is an async operation and needs an async command path —
/// deferred (documented in docs/progress.md).
/// </summary>
internal sealed class AvaloniaClipboardBridge : Clipboard.IProvider
{
    public static readonly AvaloniaClipboardBridge Instance = new();

    private IDataObject? _current;
    private TopLevel? _topLevel;
    private bool _installed;

    public void Attach(TopLevel? topLevel)
    {
        if (topLevel is not null)
        {
            _topLevel = topLevel;
        }

        if (!_installed)
        {
            _installed = true;
            Clipboard.SetProvider(this);
        }
    }

    public IDataObject? GetDataObject() => _current;

    public void SetDataObject(IDataObject data, bool copy)
    {
        _current = data;
        if (data?.GetData(DataFormats.UnicodeText) is string text && _topLevel?.Clipboard is { } clipboard)
        {
            _ = clipboard.SetTextAsync(text);
        }
    }

    public bool ContainsText() => _current?.GetDataPresent(typeof(string)) == true;
}
