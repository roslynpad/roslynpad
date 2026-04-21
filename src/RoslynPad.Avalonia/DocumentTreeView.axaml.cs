using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RoslynPad.UI;
using Avalonia.VisualTree;

namespace RoslynPad;

public partial class DocumentTreeView : UserControl
{
    private MainViewModel? _viewModel;

    public DocumentTreeView()
    {
        InitializeComponent();
        var treeView = this.Find<TreeView>("Tree");
        if (treeView != null)
        {
            treeView.DoubleTapped += OnDocumentClick;
            treeView.KeyDown += OnDocumentKeyDown;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _viewModel = DataContext as MainViewModel ?? throw new InvalidOperationException("DataContext is null");
    }

    private void OnDocumentClick(object? sender, RoutedEventArgs e)
    {
        OpenDocument(e.Source);
    }

    private void OnDocumentKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OpenDocument(e.Source);
        }
    }

    private void OpenDocument(object? source)
    {
        var item = (source as Visual)?.GetSelfAndVisualAncestors()
                .OfType<TreeViewItem>()
                .FirstOrDefault();

        if (item?.DataContext is DocumentViewModel documentViewModel)
        {
            _viewModel?.OpenDocument(documentViewModel);
        }
    }

    private static DocumentViewModel? GetDocumentFromSource(object? sender)
    {
        return (sender as MenuItem)?.DataContext as DocumentViewModel;
    }

    private void DocumentsContextMenu_OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (GetDocumentFromSource(sender) is { } documentViewModel)
        {
            _viewModel?.OpenDocumentInExplorer(documentViewModel);
        }
    }

    private async void DocumentsContextMenu_Rename_Click(object? sender, RoutedEventArgs e)
    {
        if (GetDocumentFromSource(sender) is { IsFolder: false } documentViewModel && _viewModel != null)
        {
            await _viewModel.RenameDocument(documentViewModel).ConfigureAwait(true);
        }
    }

    private async void DocumentsContextMenu_SaveAs_Click(object? sender, RoutedEventArgs e)
    {
        if (GetDocumentFromSource(sender) is { IsFolder: false } documentViewModel && _viewModel != null)
        {
            await _viewModel.SaveDocumentAs(documentViewModel).ConfigureAwait(true);
        }
    }
}
