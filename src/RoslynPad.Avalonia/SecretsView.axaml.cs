using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using DialogHostAvalonia;
using RoslynPad.UI;

namespace RoslynPad;

public partial class SecretsView : UserControl
{
    public SecretsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    void OnCopyingToClipboard(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SecretsViewModel vm)
        {
            vm.ConfirmRemoveSecret = ConfirmRemoveSecretAsync;
        }
    }

    private async ValueTask<bool> ConfirmRemoveSecretAsync(string name)
    {
        var removeButton = new Button { Content = "Remove", Margin = new Avalonia.Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Don't Remove" };

        removeButton.Click += (_, _) => DialogHost.Close(MainWindow.DialogHostIdentifier, true);
        cancelButton.Click += (_, _) => DialogHost.Close(MainWindow.DialogHostIdentifier, false);

        var content = new StackPanel
        {
            Margin = new Avalonia.Thickness(15),
            Children =
            {
                new TextBlock
                {
                    Text = $"Are you sure you want to remove the secret '{name}'?",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 0, 0, 15)
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { removeButton, cancelButton }
                }
            }
        };

        var result = await DialogHost.Show(content, MainWindow.DialogHostIdentifier).ConfigureAwait(true);
        return result is true;
    }
}
