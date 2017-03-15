using System.Composition;
using System.Windows;
using Microsoft.Win32;
using RoslynPad.UI;

namespace RoslynPad
{
    [Export(typeof(ISaveFileDialog))]
    internal class SaveFileDialogAdapter : ISaveFileDialog
    {
        private readonly SaveFileDialog _dialog;

        public SaveFileDialogAdapter()
        {
            _dialog = new SaveFileDialog();
        }

        public bool OverwritePrompt
        {
            get => _dialog.OverwritePrompt;
            set => _dialog.OverwritePrompt = value;
        }

        public bool AddExtension
        {
            get => _dialog.AddExtension;
            set => _dialog.AddExtension = value;
        }

        public string Filter
        {
            get => _dialog.Filter;
            set => _dialog.Filter = value;
        }

        public string DefaultExt
        {
            get => _dialog.DefaultExt;
            set => _dialog.DefaultExt = value;
        }

        public string FileName
        {
            get => _dialog.FileName;
            set => _dialog.FileName = value;
        }

        public bool? Show()
        {
            return _dialog.ShowDialog(Application.Current.MainWindow);
        }
    }
}