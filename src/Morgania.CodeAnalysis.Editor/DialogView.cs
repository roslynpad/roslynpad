using Avalonia;
using Avalonia.Controls;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Base class for dialog content shown through an <see cref="IDialogPresenter"/>.
/// The content calls <see cref="Close"/> to dismiss itself with a result.
/// </summary>
internal abstract class DialogView : UserControl, IDialogView
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DialogView, string?>(nameof(Title));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public event EventHandler<bool?>? CloseRequested;

    public void Close(bool? result) => CloseRequested?.Invoke(this, result);
}
