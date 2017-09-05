using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using RoslynPad.UI;

namespace RoslynPad
{
    [Export(typeof(IOpenFileDialog))]
    internal class OpenFileDialogAdapter : IOpenFileDialog
    {
        private readonly OpenFileDialog _dialog;

        public OpenFileDialogAdapter()
        {
            _dialog = new OpenFileDialog();
        }

        public bool AllowMultiple
        {
            get => _dialog.Multiselect;
            set => _dialog.Multiselect = value;
        }

        public FileDialogFilter Filter
        {
            set => _dialog.Filter = value?.ToString();
        }

        public string InitialDirectory
        {
            get => _dialog.InitialDirectory;
            set => _dialog.InitialDirectory = value;
        }

        public string FileName
        {
            get => _dialog.FileName;
            set => _dialog.FileName = value;
        }

        public IList<string> FileNames => _dialog.FileNames;

        public Task<bool> ShowAsync()
        {
            return Task.FromResult(_dialog.ShowDialog(Application.Current.MainWindow) == true);
        }
    }
}