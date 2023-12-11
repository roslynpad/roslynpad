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
    /// Interaction logic for BranchHistoryView.xaml
    /// </summary>
    public partial class BranchHistoryView : UserControl
    {
        GitBranchHistoryViewModel? viewModel;
        public BranchHistoryView()
        {
            InitializeComponent();
            this.DataContextChanged += BranchHistoryView_DataContextChanged;
        }

        private void BranchHistoryView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            viewModel = e.NewValue as GitBranchHistoryViewModel; 
            if (viewModel == null || viewModel.MainViewModel == null) return;
            if (viewModel.Type != BranchHistoryType.File) BranchHistory.ContextMenu=null;
            //if (viewModel != null && viewModel.MainViewModel != null)
            //    BranchHistory.FontSize = viewModel.MainViewModel.EditorFontSize;
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var item = BranchHistory.SelectedItem as CommitItem;
                if (item == null || viewModel==null || viewModel.MainViewModel==null) return;
                if(viewModel.Type== BranchHistoryType.Commit)
                {
                    viewModel.MainViewModel.ShowCommitDetail(item.FullId);
                }
                else
                {
                    viewModel.MainViewModel.ShowCommitFile(item.FullId, viewModel.FilePath);
                }
            }
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void OnCompareWithPrevious(object sender, RoutedEventArgs e)
        {
            var item = BranchHistory.SelectedItem as CommitItem;
            if (item == null || viewModel == null || viewModel.MainViewModel == null) return;
            if (viewModel.Type != BranchHistoryType.File) return;
            int index = viewModel.Commits.IndexOf(item);
            if (index < 0) return;
            index++;
            if (index >= viewModel.Commits.Count) return;
            var pre = viewModel.Commits[index];
            viewModel.MainViewModel.CompareFileWithCommits(item.FullId, viewModel.FilePath, pre.FullId);
        }

        private void OnCompareWithCurrent(object sender, RoutedEventArgs e)
        {
            var item = BranchHistory.SelectedItem as CommitItem;
            if (item == null || viewModel == null || viewModel.MainViewModel == null) return;
            if (viewModel.Type != BranchHistoryType.File) return;
            viewModel.MainViewModel.CompareFileWithCurrent(item.FullId, viewModel.FilePath);
        }
    }
}
