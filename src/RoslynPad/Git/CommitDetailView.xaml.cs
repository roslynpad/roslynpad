using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for CommitDetailView.xaml
    /// </summary>
    public partial class CommitDetailView : UserControl
    {
        CommitChangesViewModel? viewModel;
        public CommitDetailView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            viewModel = args.NewValue as CommitChangesViewModel;
        }
        private void OnComparePrevious(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).DataContext is CommitChangesViewModel gitChangeModel)
            {
                string path = "";
                if (gitChangeModel.IsFolder)
                {
                }
                else
                {
                    path = gitChangeModel.Path;
                    if (viewModel != null && viewModel.MainViewModel != null)
                    {
                        viewModel.MainViewModel.CompareFileWithPreviouse(viewModel.CommitId, path);
                    }
                }
            }
        }
        private void OnCompareCurrent(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).DataContext is CommitChangesViewModel gitChangeModel)
            {
                string path = "";
                if (gitChangeModel.IsFolder)
                {
                }
                else
                {
                    path = gitChangeModel.Path;
                    if (viewModel != null && viewModel.MainViewModel != null)
                    {
                        viewModel.MainViewModel.CompareFileWithCurrent(viewModel.CommitId, path);
                    }
                }
            }
        }
        private void OnDocumentClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (((FrameworkElement)e.Source).DataContext is CommitChangesViewModel gitChangeModel)
                {
                    string path = "";
                    if (gitChangeModel.IsFolder)
                    {
                    }
                    else
                    {
                        path = gitChangeModel.Path;
                        if (viewModel != null && viewModel.MainViewModel != null)
                        {
                            viewModel.MainViewModel.ShowCommitFile(viewModel.CommitId, path);
                        }
                    }
                }
            }
        }
    }
}
