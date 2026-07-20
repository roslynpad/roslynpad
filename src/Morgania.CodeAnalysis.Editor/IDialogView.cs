namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// A dialog shown through an <see cref="IDialogPresenter"/>. Implementations are Avalonia
/// controls; presenters display them as visual content.
/// </summary>
public interface IDialogView
{
    string? Title { get; }

    /// <summary>
    /// Raised when the dialog wants to close; the presenter completes
    /// <see cref="IDialogPresenter.ShowDialogAsync"/> with the result.
    /// </summary>
    event EventHandler<bool?>? CloseRequested;
}
