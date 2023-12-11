using ICSharpCode.AvalonEdit.Folding;
using System;
using System.Collections.Generic;
using System.IO;
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
using Path = System.IO.Path;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for GitFileViewer.xaml
    /// </summary>
    public partial class GitFileViewer : UserControl
    {
        GitCommitFileViewModel? viewModel;
        FoldingManager? foldingManager;
        BraceFoldingStrategy foldingStrategy = new BraceFoldingStrategy();
        public GitFileViewer()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            viewModel = e.NewValue as GitCommitFileViewModel;
            if (viewModel == null) return;
            if(viewModel.MainViewModel!=null)
                 textEditor.FontSize = viewModel.MainViewModel.EditorFontSize;
            var ext = Path.GetExtension(viewModel.FilePath);
            if (ext.Equals("csx", StringComparison.OrdinalIgnoreCase) || ext.Equals("cs", StringComparison.OrdinalIgnoreCase))
            {

            }
            else
            {
                var manager = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance;
                var def = manager.GetDefinitionByExtension(ext);
                if (def != null)
                    textEditor.SyntaxHighlighting = def;
            }
            foldingManager = ICSharpCode.AvalonEdit.Folding.FoldingManager.Install(textEditor.TextArea);
            textEditor.Text = viewModel.Blob.GetContentText();
            foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
        }
    }
}
