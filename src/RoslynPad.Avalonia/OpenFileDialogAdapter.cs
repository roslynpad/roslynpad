using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
            get => _dialog.AllowMultiple;
            set => _dialog.AllowMultiple = value;
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

        public string InitialDirectory
        {
            get => _dialog.Directory;
            set => _dialog.Directory = value;
        }

        public string FileName
        {
            get => _dialog.InitialFileName;
            set => _dialog.InitialFileName = value;
        }

        public Task<string[]?> ShowAsync()
        {
            var active = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows.First(w => w.IsActive);
            return _dialog.ShowAsync(active);
        }
    }
}