using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.AvalonDock;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            InitializeComponent();
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
            documentViewModel.OpenDocumentCommand.Execute();
        }

        private void DockingManager_OnDocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            _viewModel.CloseDocument((OpenDocumentViewModel) e.Document.Content);
        }
    }
}
