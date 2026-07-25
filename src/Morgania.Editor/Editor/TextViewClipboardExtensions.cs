#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using Avalonia.Controls;
using Avalonia.Input;

/// <summary>
/// The editor's OS-clipboard integration, over the clipboard of the view's TopLevel. The
/// OS clipboard is the single source of truth — there is no in-process store — but it is
/// async-only (X11 paste is inter-process IPC) while the editor-operations clipboard APIs
/// are synchronous, so each direction bridges differently: copies push fire-and-forget,
/// and paste callers go through <see cref="PasteFromClipboardAsync"/>, which fetches the
/// clipboard first and primes the view with the snapshot (a <see cref="PendingClipboardPaste"/>
/// in the view's property bag) for the duration of the synchronous paste dispatch.
/// </summary>
public static class TextViewClipboardExtensions
{
    /// <summary>
    /// Fetches the OS clipboard and invokes <paramref name="pasteAction"/> — typically
    /// <c>IEditorOperations.Paste</c>, or a commanding-chain execution ending in it — with
    /// the data available to it. Application data formats come back as presence markers,
    /// mirroring <see cref="SetClipboardText"/>, so the operations layer's line/box
    /// cut-copy tags survive the OS round trip. Does not invoke the action when the
    /// clipboard is unreachable or holds no text. Must be called from the UI thread.
    /// </summary>
    public static async Task PasteFromClipboardAsync(this IWpfTextView view, Action pasteAction)
    {
        if (TopLevel.GetTopLevel(view.VisualElement)?.Clipboard is not { } clipboard)
        {
            return;
        }

        PendingClipboardPaste? pending = null;
        try
        {
            // ConfigureAwait(true): the paste dispatch must resume on the UI thread.
            using var transfer = await clipboard.TryGetDataAsync().ConfigureAwait(true);
            if (transfer is not null && await transfer.TryGetTextAsync().ConfigureAwait(true) is { } text)
            {
                pending = new PendingClipboardPaste(
                    text,
                    [.. transfer.Formats.Where(f => f.Kind == DataFormatKind.Application).Select(f => f.Identifier)]);
            }
        }
        catch
        {
            // Clipboard unreachable (contended, or the X11 selection owner vanished);
            // paste degrades to a no-op.
            return;
        }

        if (pending is null)
        {
            return;
        }

        view.Properties[typeof(PendingClipboardPaste)] = pending;
        try
        {
            pasteAction();
        }
        finally
        {
            view.Properties.RemoveProperty(typeof(PendingClipboardPaste));
        }
    }

    /// <summary>
    /// Pushes text to the OS clipboard, with each application format as a presence
    /// marker. Fire-and-forget: the copy APIs are synchronous, and a failed push (the
    /// Win32 clipboard can be transiently locked by another process) must not fault the
    /// editing operation that already completed.
    /// </summary>
    internal static void SetClipboardText(this ITextView view, string text, IReadOnlyList<string> applicationFormats)
    {
        if (view is not IWpfTextView wpfView ||
            TopLevel.GetTopLevel(wpfView.VisualElement)?.Clipboard is not { } clipboard)
        {
            return;
        }

        var item = new DataTransferItem();
        item.SetText(text);
        foreach (var format in applicationFormats)
        {
            item.Set(DataFormat.CreateBytesApplicationFormat(format), [1]);
        }

        var transfer = new DataTransfer();
        transfer.Add(item);
        _ = PushAsync(clipboard, transfer);

        static async Task PushAsync(Avalonia.Input.Platform.IClipboard clipboard, DataTransfer transfer)
        {
            try
            {
                await clipboard.SetDataAsync(transfer).ConfigureAwait(false);
            }
            catch
            {
                // See the fire-and-forget note above.
            }
        }
    }
}

/// <summary>
/// The OS-clipboard snapshot primed into a view's property bag for the duration of one
/// synchronous paste dispatch.
/// </summary>
internal sealed record PendingClipboardPaste(string Text, IReadOnlyList<string> ApplicationFormats);
