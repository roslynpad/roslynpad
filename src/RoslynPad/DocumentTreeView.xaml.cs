using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RoslynPad.UI;

namespace RoslynPad;

public partial class DocumentTreeView
{
    private MainViewModel _viewModel;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public DocumentTreeView()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        _viewModel = (MainViewModel)e.NewValue;
    }

    private void OnViewHistoryClick(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)e.Source).DataContext is DocumentViewModel documentViewModel)
        {
            if (documentViewModel.IsFolder) return;
            var file = documentViewModel.Path;
            _viewModel.ShowFileHistory(file);
        }
    }
    private async void DocumentsContextMenu_Delete_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)e.Source).DataContext is DocumentViewModel documentViewModel)
        {
            if (documentViewModel.IsFolder)
            {
                if (MessageBox.Show("Are you sure to delete this folder and all the files inside it?", "delete confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question)
                    != MessageBoxResult.OK)
                    return;
                Directory.Delete(documentViewModel.Path, true);
                _viewModel.DocumentRoot.Children?.Remove(documentViewModel);
            }
            else
            {
                if (MessageBox.Show("Are you sure to delete this file?", "delete confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question)
                    != MessageBoxResult.OK)
                    return;
                for (int i = 0; i < _viewModel.OpenDocuments.Count; i++)
                {
                    var doc = _viewModel.OpenDocuments[i];
                    if (doc.Document != null && doc.Document.Path == documentViewModel.Path)
                    {
                        await _viewModel.CloseDocument(doc).ConfigureAwait(true);
                        break;
                    }
                }
                var file = documentViewModel.Path;
                File.Delete(file);
                _viewModel.DocumentRoot.Children?.Remove(documentViewModel);
            }
        }
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
            if (documentViewModel.IsFolder)
            {
                _ = Task.Run(() => Process.Start(new ProcessStartInfo { FileName = documentViewModel.Path, UseShellExecute = true }));
            }
            else
            {
                _ = Task.Run(() => Process.Start("explorer.exe", "/select," + documentViewModel.Path));
            }
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
        if (value is IList list)
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
