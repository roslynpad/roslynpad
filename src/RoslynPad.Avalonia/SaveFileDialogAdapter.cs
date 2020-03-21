using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
            get => false;
            set { }
        }

        public bool AddExtension
        {
            get => false;
            set { }
        }

        public UI.FileDialogFilter Filter
        {
            set
            {
                _dialog.Filters.Clear();
                if (value == null)
                {
                    return;
                }

                _dialog.Filters.Add(new Avalonia.Controls.FileDialogFilter
                {
                    Name = value.Header,
                    Extensions = value.Extensions.ToList()
                });
            }
        }

        public string DefaultExt
        {
            get => _dialog.DefaultExtension;
            set => _dialog.DefaultExtension = value;
        }

        public string FileName
        {
            get => _dialog.InitialFileName;
            set => _dialog.InitialFileName = value;
        }

        public Task<string?> ShowAsync()
        {
            var active = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.First(w => w.IsActive);
            return _dialog.ShowAsync(null);
        }
    }
}