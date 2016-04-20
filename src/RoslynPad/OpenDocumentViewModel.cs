using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Host;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad
{
    internal class OpenDocumentViewModel : NotificationObject
    {
        private readonly object _resultsLock;
        private ExecutionHost _executionHost;
        private readonly string _workingDirectory;

        public ObservableCollection<ResultObjectViewModel> Results { get; }

        public DocumentViewModel Document { get; private set; }

        public OpenDocumentViewModel(MainViewModel mainViewModel, DocumentViewModel document)
        {
            Document = document;
            MainViewModel = mainViewModel;

            var roslynHost = mainViewModel.RoslynHost;

            _workingDirectory = Document != null
                ? Path.GetDirectoryName(Document.Path)
                : MainViewModel.DocumentRoot.Path;

            _executionHost = new ExecutionHost("RoslynPad.Host.exe", _workingDirectory,
                roslynHost.DefaultReferences.OfType<PortableExecutableReference>().Select(x => x.FilePath),
                roslynHost.DefaultImports, mainViewModel.NuGetProvider, mainViewModel.ChildProcessManager);
            _executionHost.Dumped += AddResult;

            _resultsLock = new object();
            Results = new ObservableCollection<ResultObjectViewModel>();
            BindingOperations.EnableCollectionSynchronization(Results, _resultsLock);

            SaveCommand = new DelegateCommand(Save);
            RunCommand = new DelegateCommand(Run);
            RestartHostCommand = new DelegateCommand(RestartHost);
        }

        private Task RestartHost()
        {
            return _executionHost.ResetAsync();
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

        public async Task Initialize(SourceTextContainer sourceTextContainer, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated)
        {
            var roslynHost = MainViewModel.RoslynHost;
            // ReSharper disable once AssignNullToNotNullAttribute
            DocumentId = roslynHost.AddDocument(sourceTextContainer, _workingDirectory, onDiagnosticsUpdated, onTextUpdated);
            await _executionHost.ResetAsync().ConfigureAwait(false);
        }

        public DocumentId DocumentId { get; private set; }

        public MainViewModel MainViewModel { get; }

        public NuGetViewModel NuGet => MainViewModel.NuGet;

        public string Title => Document?.Name ?? "New";

        public DelegateCommand SaveCommand { get; }

        public DelegateCommand RunCommand { get; }

        public DelegateCommand RestartHostCommand { get; }

        private async Task Run()
        {
            lock (_resultsLock)
            {
                Results.Clear();
            }

            try
            {
                var code = await MainViewModel.RoslynHost.GetDocument(DocumentId).GetTextAsync().ConfigureAwait(true);
                await _executionHost.ExecuteAsync(code.ToString()).ConfigureAwait(true);
                //if (result != null)
                //{
                //    AddResult(result);
                //}
            }
            catch (CompilationErrorException ex)
            {
                lock (_resultsLock)
                {
                    foreach (var diagnostic in ex.Diagnostics)
                    {
                        Results.Add(new ResultObjectViewModel(ResultObject.Create(diagnostic)));
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult(ex);
            }
        }


        public void AddResult(object o)
        {
            lock (_resultsLock)
            {
                Results.Add(new ResultObjectViewModel(o as ResultObject ?? ResultObject.Create(o)));
            }
        }

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

        public void Close()
        {
            _executionHost?.Dispose();
            _executionHost = null;
        }
    }
}