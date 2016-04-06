using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal class OpenDocumentViewModel : NotificationObject
    {
        public DocumentViewModel Document { get; private set; }

        public OpenDocumentViewModel(MainViewModel mainViewModel, DocumentViewModel document)
        {
            Document = document;
            MainViewModel = mainViewModel;

            SaveCommand = new DelegateCommand(Save);
        }

        private async Task Save()
        {
            if (Document == null && PromptForDocument != null)
            {
                var documentName = await PromptForDocument().ConfigureAwait(true);
                if (documentName != null)
                {
                    Document = MainViewModel.AddDocument(documentName);
                    OnPropertyChanged(nameof(Title));
                }
            }
            if (Document != null)
            {
                await SaveDocument().ConfigureAwait(true);
            }
        }

        public Func<Task<string>> PromptForDocument { get; set; }

        private async Task SaveDocument()
        {
            var text = await MainViewModel.RoslynHost.GetDocument(DocumentId).GetTextAsync().ConfigureAwait(false);
            using (var writer = new StreamWriter(Document.Path, append: false))
            {
                foreach (var line in text.Lines)
                {
                    await writer.WriteLineAsync(line.ToString()).ConfigureAwait(false);
                }
            }
        }

        public Task Initialize(SourceTextContainer sourceTextContainer, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated)
        {
            var roslynHost = MainViewModel.RoslynHost;
            DocumentId = roslynHost.AddDocument(sourceTextContainer, onDiagnosticsUpdated, onTextUpdated);
            return Task.CompletedTask;
        }

        public DocumentId DocumentId { get; private set; }

        public MainViewModel MainViewModel { get; }

        public NuGetViewModel NuGet => MainViewModel.NuGet;

        public string Title => Document?.Name ?? "New";

        public DelegateCommand SaveCommand { get; }

        public async Task<string> LoadText()
        {
            if (Document == null)
            {
                return string.Empty;
            }
            using (var fileStream = new StreamReader(Document.Path))
            {
                return await fileStream.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}