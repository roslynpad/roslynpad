#pragma warning disable CS8618

using RoslynPad.UI;

namespace RoslynPad;

public partial class DocumentTreeView
{
    private MainViewModel _viewModel;

    public DocumentTreeView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModel = (MainViewModel)e.NewValue;
    }

    private void OnDocumentClick(object? sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            OpenDocument(e.Source);
        }
    }

    private void OnDocumentKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OpenDocument(e.Source);
        }
    }

    private void OpenDocument(object source)
    {
        var documentViewModel = (DocumentViewModel)((FrameworkElement)source).DataContext;
        _viewModel.OpenDocument(documentViewModel);
    }

    private void DocumentsContextMenu_OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)e.Source).DataContext is DocumentViewModel documentViewModel)
        {
            _viewModel.OpenDocumentInExplorer(documentViewModel);
        }
    }

    private async void DocumentsContextMenu_Rename_Click(object? sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)e.Source).DataContext is DocumentViewModel documentViewModel && !documentViewModel.IsFolder)
        {
            await _viewModel.RenameDocument(documentViewModel).ConfigureAwait(true);
        }
    }

    private async void DocumentsContextMenu_SaveAs_Click(object? sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)e.Source).DataContext is DocumentViewModel documentViewModel && !documentViewModel.IsFolder)
        {
            await _viewModel.SaveDocumentAs(documentViewModel).ConfigureAwait(true);
        }
    }

    private void Search_OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _viewModel.ClearSearchCommand.Execute();
                break;
            case Key.Enter:
                _viewModel.SearchCommand.Execute();
                break;
        }
    }

    private bool FilterCollectionViewSourceConverter_OnFilter(object arg) => ((DocumentViewModel)arg).IsSearchMatch;
}

internal class FilterCollectionViewConverter : IValueConverter
{
    public string? FilterProperty { get; set; }

    public event Predicate<object>? Filter;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Collections.IList list)
        {
            var collectionView = new ListCollectionView(list)
            {
                IsLiveFiltering = true,
                LiveFilteringProperties = { FilterProperty },
                Filter = Filter
            };

            return collectionView;
        }

        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
