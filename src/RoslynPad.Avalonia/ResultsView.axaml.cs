using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using RoslynPad.Build;
using RoslynPad.Utilities;

namespace RoslynPad;

partial class ResultsView : UserControl
{
    public static readonly StyledProperty<IDelegateCommand?> CopyValueProperty =
        AvaloniaProperty.Register<ResultsView, IDelegateCommand?>(nameof(CopyValue));

    public static readonly StyledProperty<IDelegateCommand?> CopyAllValuesProperty =
        AvaloniaProperty.Register<ResultsView, IDelegateCommand?>(nameof(CopyAllValues));

    public IDelegateCommand? CopyValue
    {
        get => GetValue(CopyValueProperty);
        set => SetValue(CopyValueProperty, value);
    }

    public IDelegateCommand? CopyAllValues
    {
        get => GetValue(CopyAllValuesProperty);
        set => SetValue(CopyAllValuesProperty, value);
    }

    public ResultsView()
    {
        CopyValue = new DelegateCommand(CopyValueToClipboardAsync);
        CopyAllValues = new DelegateCommand(CopyAllValuesToClipboardAsync);
        InitializeComponent();
    }

    private IClipboard? GetClipboard() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Clipboard;

    private async Task CopyValueToClipboardAsync()
    {
        if (GetClipboard() is { } clipboard &&
            ResultTree.SelectedItem is IResultObject result &&
            result.Value is not null)
        {
            await clipboard.SetTextAsync(result.Value).ConfigureAwait(true);
        }
    }

    private async Task CopyAllValuesToClipboardAsync()
    {
        if (GetClipboard() is not { } clipboard ||
            DataContext is not UI.OpenDocumentViewModel vm)
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var result in vm.Results)
        {
            result.WriteTo(builder);
            builder.AppendLine();
        }

        if (builder.Length > 0)
        {
            await clipboard.SetTextAsync(builder.ToString()).ConfigureAwait(true);
        }
    }
}
