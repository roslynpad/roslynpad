using System.Composition;
using DialogHostAvalonia;
using Morgania.CodeAnalysis.Editor;

namespace RoslynPad.Roslyn;

/// <summary>
/// Presents editor dialogs (change signature, extract interface, pick members) in the main
/// window's <see cref="DialogHost"/> overlay instead of separate modal windows. Composed into
/// the VS-MEF editor graph through <see cref="UI.MainViewModel.CompositionAssemblies"/>.
/// </summary>
[Export(typeof(IDialogPresenter)), Shared]
internal sealed class DialogPresenter : IDialogPresenter
{
    public async Task<bool?> ShowDialogAsync(IDialogView dialog)
    {
        void OnCloseRequested(object? sender, bool? result) =>
            DialogHost.Close(MainWindow.DialogHostIdentifier, result);

        dialog.CloseRequested += OnCloseRequested;
        try
        {
            return await DialogHost.Show(dialog, MainWindow.DialogHostIdentifier).ConfigureAwait(true) as bool?;
        }
        finally
        {
            dialog.CloseRequested -= OnCloseRequested;
        }
    }
}
