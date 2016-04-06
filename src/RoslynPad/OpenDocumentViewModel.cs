using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal class OpenDocumentViewModel : NotificationObject
    {
        public DocumentViewModel Document { get; }

        public OpenDocumentViewModel(MainViewModel mainViewModel, DocumentViewModel document)
        {
            Document = document;
            MainViewModel = mainViewModel;

            SaveCommand = new DelegateCommand((Action)Save);
        }

        private void Save()
        {

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
    }
}