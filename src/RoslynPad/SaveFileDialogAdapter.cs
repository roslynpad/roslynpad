using System.Composition;
using System.Threading.Tasks;
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

        public FileDialogFilter Filter
        {
            set => _dialog.Filter = value.ToString();
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

        public Task<string?> ShowAsync()
        {
            if (_dialog.ShowDialog(Application.Current.MainWindow) == true)
            {
                return Task.FromResult<string?>(_dialog.FileName);
            }

            return Task.FromResult<string?>(null);
        }
    }
}