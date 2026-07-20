namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Presents editor dialogs (refactoring options, change signature, etc.). Host applications
/// may export an implementation to show dialogs inside their own chrome (e.g. an in-window
/// overlay); when none is exported, dialogs open as modal windows.
/// </summary>
public interface IDialogPresenter
{
    /// <summary>
    /// Shows the dialog and completes with the result it raised
    /// <see cref="IDialogView.CloseRequested"/> with, or <see langword="null"/> if it was dismissed.
    /// </summary>
    Task<bool?> ShowDialogAsync(IDialogView dialog);
}
