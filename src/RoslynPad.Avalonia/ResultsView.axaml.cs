using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RoslynPad.Build;
using RoslynPad.Utilities;

namespace RoslynPad;

partial class ResultsView : UserControl
{
    public static readonly StyledProperty<IDelegateCommand?> CopyValueProperty =
        AvaloniaProperty.Register<ResultsView, IDelegateCommand?>(nameof(CopyValue));

    public static readonly StyledProperty<IDelegateCommand?> CopyAllValuesProperty =
        AvaloniaProperty.Register<ResultsView, IDelegateCommand?>(nameof(CopyAllValues));

    public static FuncValueConverter<string?, object?> SeverityToIconConverter { get; } =
        new(severity => Application.Current?.FindResource(
            severity == "Warning" ? "WarningMarker" : "ExceptionMarker"));

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
        CopyAllValues = new DelegateCommand(() => CopyAllResultsToClipboardAsync(withChildren: true));
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Sync header scroll with tree's horizontal scroll
        if (ResultTree.FindDescendantOfType<ScrollViewer>() is { } scrollViewer)
        {
            scrollViewer.PropertyChanged += (_, args) =>
            {
                if (args.Property == ScrollViewer.OffsetProperty)
                {
                    HeaderScroll.Offset = new Vector(scrollViewer.Offset.X, 0);
                }
            };
        }
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

    private async Task CopyValueWithChildrenToClipboardAsync()
    {
        if (GetClipboard() is { } clipboard &&
            ResultTree.SelectedItem is IResultObject result)
        {
            var text = result.ToString();
            if (text is not null)
            {
                await clipboard.SetTextAsync(text).ConfigureAwait(true);
            }
        }
    }

    private async Task CopyAllResultsToClipboardAsync(bool withChildren)
    {
        if (GetClipboard() is not { } clipboard ||
            DataContext is not UI.OpenDocumentViewModel vm)
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var result in vm.Results)
        {
            if (withChildren)
            {
                result.WriteTo(builder);
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine(result.Value);
            }
        }

        if (builder.Length > 0)
        {
            await clipboard.SetTextAsync(builder.ToString()).ConfigureAwait(true);
        }
    }

    private void CopyValueClick(object? sender, RoutedEventArgs e) =>
        _ = CopyValueToClipboardAsync();

    private void CopyValueWithChildrenClick(object? sender, RoutedEventArgs e) =>
        _ = CopyValueWithChildrenToClipboardAsync();

    private void CopyAllValuesClick(object? sender, RoutedEventArgs e) =>
        _ = CopyAllResultsToClipboardAsync(withChildren: false);

    private void CopyAllValuesWithChildrenClick(object? sender, RoutedEventArgs e) =>
        _ = CopyAllResultsToClipboardAsync(withChildren: true);

    private void ResultTree_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual visual &&
            visual.FindAncestorOfType<TreeViewItem>()?.DataContext is IResultWithLineNumber result &&
            DataContext is UI.OpenDocumentViewModel vm)
        {
            vm.TryJumpToLine(result);
        }
    }
}
