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
    /// Interaction logic for GitCompareFileView.xaml
    /// </summary>
    public partial class GitCompareFileView : UserControl
    {
        GitFileCompareViewModel? viewModel;
        public GitCompareFileView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            viewModel = e.NewValue as GitFileCompareViewModel;
            if (viewModel == null) return;
            var leftMargin = new DiffInfoMargin (viewModel.OldDocument );
            var leftBackgroundRenderer = new DiffLineBackgroundRenderer(viewModel.OldDocument);
            LeftEditor.TextArea.LeftMargins.Add(leftMargin);
            LeftEditor.TextArea.TextView.BackgroundRenderers.Add(leftBackgroundRenderer);
            LeftEditor.Text = viewModel.OldDocument.Text;
            LeftTitle.Content = viewModel.OldDocument.Title;
            LeftEditor.TextArea.MouseWheel += OnEditorMouseWheel;
            LeftEditor.FontSize = viewModel.MainViewModel.EditorFontSize;

            var rightMargin = new DiffInfoMargin(viewModel.NewDocument);
            var rightBackgroundRenderer = new DiffLineBackgroundRenderer(viewModel.NewDocument);
            RightEditor.TextArea.TextView.BackgroundRenderers.Add(rightBackgroundRenderer);
            RightEditor.TextArea.LeftMargins.Add(rightMargin);
            RightEditor.Text = viewModel.NewDocument.Text;
            RightTitle.Content = viewModel.NewDocument.Title;
            RightEditor.TextArea.MouseWheel += OnEditorMouseWheel;
            RightEditor.FontSize = viewModel.MainViewModel.EditorFontSize;
        }

        private void OnEditorMouseWheel(object sender, MouseWheelEventArgs e)
        {
            EditorScrollViewer.ScrollToVerticalOffset(EditorScrollViewer.VerticalOffset - e.Delta/5);
        }
    }
}
