using RoslynPad.UI;
using System;
using System.Composition;
using System.Threading.Tasks;

namespace RoslynPad
{
    [Export(typeof(ISaveDocumentDialog))]
    public class SaveDocumentDialog : ISaveDocumentDialog
    {
        public string DocumentName { get; set; }
        public SaveResult Result { get; private set; }
        public bool AllowNameEdit { get; set; }
        public bool ShowDontSave { get; set; }
        public string FilePath { get; }
        public Func<string, string> FilePathFactory { get; set; }

        private readonly ISaveFileDialog _saveFileDialog;

        [ImportingConstructor]
        public SaveDocumentDialog(ISaveFileDialog saveFileDialog)
        {
            _saveFileDialog = saveFileDialog;
        }

        public void Close()
        {
        }

        public async Task ShowAsync()
        {
            var name = DocumentName;
            if (string.IsNullOrEmpty(name))
            {
                name = "Untitled";
            }

            _saveFileDialog.FileName = FilePathFactory.Invoke(name);
            if (await _saveFileDialog.ShowAsync().ConfigureAwait(true) != null)
            {
                Result = SaveResult.Save;
            }

            // TODO: this is a temporary hack! there's no way to cancel and keep the document now
            Result = SaveResult.DontSave;
        }
    }
}
