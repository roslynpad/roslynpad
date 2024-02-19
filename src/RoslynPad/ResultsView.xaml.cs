using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RoslynPad.Build;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Interaction logic for ResultsView.xaml
/// </summary>
public partial class ResultsView : UserControl
{
    private IResultObject? _contextMenuResultObject;
    private OpenDocumentViewModel? _viewModel;

    public ResultsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public OpenDocumentViewModel ViewModel => _viewModel.NotNull();

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs args)
    {
        _viewModel = (OpenDocumentViewModel)args.NewValue;
    }

    private void CopyCommand(object? sender, ExecutedRoutedEventArgs e)
    {
        CopyToClipboard(e.OriginalSource);
    }

    private void CopyClick(object? sender, RoutedEventArgs e)
    {
        CopyToClipboard(sender);
    }

    private void CopyToClipboard(object? sender)
    {
        var result = (sender as FrameworkElement)?.DataContext as IResultObject ??
                    _contextMenuResultObject;

        if (result != null)
        {
            Clipboard.SetText(ReferenceEquals(sender, CopyValueWithChildren) ? result.ToString() : result.Value);
        }
    }

    private void CopyAllClick(object? sender, RoutedEventArgs e)
    {
        var withChildren = ReferenceEquals(sender, CopyAllValuesWithChildren);

        CopyAllResultsToClipboard(withChildren);
    }

    private void CopyAllResultsToClipboard(bool withChildren)
    {
        var builder = new StringBuilder();
        foreach (var result in ViewModel.Results)
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
            Clipboard.SetText(builder.ToString());
        }
    }

    private void ResultTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                CopyAllResultsToClipboard(withChildren: true);
            }
            else
            {
                CopyToClipboard(e.OriginalSource);
            }
        }
        else if (e.Key == Key.Enter)
        {
            TryJumpToLine(e.OriginalSource);
        }
    }

    private void ResultTreeDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        TryJumpToLine(e.OriginalSource);
    }

    private void TryJumpToLine(object source)
    {
        var dataContext = (source as FrameworkElement)?.DataContext;
        if (dataContext is not IResultWithLineNumber result)
        {
            return;
        }

        ViewModel.TryJumpToLine(result);
    }

    private void ResultTreePreviewMouseWheel(object? sender, MouseWheelEventArgs args)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            var fontSize = ResultTree.FontSize + (args.Delta > 0 ? 1 : -1);
            if (!MainViewModel.IsValidFontSize(fontSize))
            {
                return;
            }

            ResultTree.FontSize = fontSize;
            args.Handled = true;
        }
    }

    private void ResultTree_OnContextMenuOpening(object? sender, ContextMenuEventArgs e)
    {
        // keyboard-activated
        if (e.CursorLeft < 0 || e.CursorTop < 0)
        {
            _contextMenuResultObject = ResultTree.SelectedItem as IResultObject;
        }
        else
        {
            _contextMenuResultObject = (e.OriginalSource as FrameworkElement)?.DataContext as IResultObject;
        }

        var isResult = _contextMenuResultObject != null;
        CopyValue.IsEnabled = isResult;
        CopyValueWithChildren.IsEnabled = isResult;
    }

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
    }
}
