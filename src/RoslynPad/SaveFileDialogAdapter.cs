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
            get { return _dialog.OverwritePrompt; }
            set { _dialog.OverwritePrompt = value; }
        }

        public bool AddExtension
        {
            get { return _dialog.AddExtension; }
            set { _dialog.AddExtension = value; }
        }

        public string Filter
        {
            get { return _dialog.Filter; }
            set { _dialog.Filter = value; }
        }

        public string DefaultExt
        {
            get { return _dialog.DefaultExt; }
            set { _dialog.DefaultExt = value; }
        }

        public string FileName
        {
            get { return _dialog.FileName; }
            set { _dialog.FileName = value; }
        }

        public bool? Show()
        {
            return _dialog.ShowDialog(Application.Current.MainWindow);
        }
    }
}