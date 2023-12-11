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
    /// Interaction logic for CommitView.xaml
    /// </summary>
    public partial class CommitView : UserControl
    {
        GitChangesViewModel? viewModel;
        public CommitView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            viewModel = args.NewValue as GitChangesViewModel;
        }

         void ExpandAll(System.Windows.Controls.TreeView treeView)
        {
            ExpandAllItems(treeView);
        }

         void ExpandAllItems(ItemsControl control)
        {
            if (control == null) return;
            foreach (Object item in control.Items)
            {
                System.Windows.Controls.TreeViewItem? treeItem = control.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeItem == null || !treeItem.HasItems)continue;
                treeItem.IsExpanded = true;
                ExpandAllItems(treeItem as ItemsControl);
            }
        }
        private void OnDocumentClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (((FrameworkElement)e.Source).DataContext is GitChangesViewModel gitChangeModel)
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
                            viewModel.MainViewModel.OpenDocument(path);
                        }
                    }
                }
            }
        }
        private void GitCompareClicked(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).DataContext is GitChangesViewModel gitChangeModel)
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
                        viewModel.MainViewModel.CompareFile(path);
                    }
                }
            }
        }
        private void GitItemOpenClicked(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).DataContext is GitChangesViewModel gitChangeModel)
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
                        viewModel.MainViewModel.OpenDocument(path);
                    }
                }
            }
        }
        private  void GitItemIgnoreClicked(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.Source).DataContext is GitChangesViewModel gitChangeModel)
            {
                string path = "";
                if (gitChangeModel.IsFolder)
                {
                    path = gitChangeModel.Path + "/";
                }
                else
                {
                    path = gitChangeModel.Path;
                }
                if(viewModel != null && viewModel.MainViewModel != null)
                {
                    viewModel.MainViewModel.GitCommit(viewModel, path);
                }
            }
        }
        private void OnCommit(object sender, RoutedEventArgs e)
        {
            if(viewModel != null && viewModel.MainViewModel!=null)
            {
                viewModel.MainViewModel.GitCommit(viewModel, CommitComment.Text);
                CommitComment.IsEnabled = false;
                CommitButton.IsEnabled = false;
            }
        }
    }
}
