using System.Windows;
using System.Windows.Controls;
using Avalon.Windows.Controls;
using RoslynPad.UI;

namespace RoslynPad;

public partial class SecretsView : UserControl
{
    public SecretsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is SecretsViewModel vm)
        {
            vm.ConfirmRemoveSecret = ConfirmRemoveSecretAsync;
        }
    }

    private ValueTask<bool> ConfirmRemoveSecretAsync(string name)
    {
        var dialog = new TaskDialog
        {
            Header = "Remove Secret",
            Content = $"Are you sure you want to remove the secret '{name}'?",
            Buttons =
            {
                new TaskDialogButtonData(1, "_Remove", content: null),
                new TaskDialogButtonData(2, "_Don't Remove", content: null, isDefault: true)
            }
        };

        dialog.SetResourceReference(BackgroundProperty, SystemColors.WindowBrushKey);
        dialog.ShowInline(this);

        return new(dialog.Result.ButtonData?.Value == 1);
    }

    private void DisableCopyCommand(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Copy)
        {
            e.Handled = DataContext is SecretItemViewModel { IsRevealed: false };
        }
    }
}
