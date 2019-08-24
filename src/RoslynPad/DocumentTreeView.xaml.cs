using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RoslynPad.UI;

namespace RoslynPad
{
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

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel = (MainViewModel)e.NewValue;
        }

        private void OnDocumentClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                OpenDocument(e.Source);
            }
        }

        private void OnDocumentKeyDown(object sender, KeyEventArgs e)
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

        private void DocumentsContextMenu_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).DataContext is DocumentViewModel documentViewModel)
            {
                if (documentViewModel.IsFolder)
                {
                    Task.Run(() => Process.Start(documentViewModel.Path));
                }
                else
                {
                    Task.Run(() => Process.Start("explorer.exe", "/select," + documentViewModel.Path));
                }
            }
        }

        private void Search_OnKeyDown(object sender, KeyEventArgs e)
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
                    LiveFilteringProperties = { FilterProperty }
                };

                collectionView.Filter = Filter;

                return collectionView;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
