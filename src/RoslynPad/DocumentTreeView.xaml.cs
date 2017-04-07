using System.Diagnostics;
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

        public DocumentTreeView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel = e.NewValue as MainViewModel;
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

        private void DocumentsSource_OnFilter(object sender, FilterEventArgs e)
        {
            e.Accepted = ((DocumentViewModel)e.Item).IsSearchMatch;
        }
    }
}
