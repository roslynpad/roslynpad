using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Composition.Hosting;
using System.IO;
using Microsoft.Practices.ServiceLocation;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.SourceEditor;
using RoslynPad.UI;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace RoslynPad.Gtk
{
    internal class Program
    {
        private static int Main()
        {
            Environment.SetEnvironmentVariable("MONODEVELOP_PROFILE", Directory.GetCurrentDirectory());
            BrandingService.ApplicationName = "RoslynPad";
            BrandingService.ApplicationLongName = "RoslynPad";

            EditorFactory.Initialize();

            var startup = new IdeStartup();
            return startup.Run(() =>
            {
                if (Platform.IsWindows)
                {
                    FontService.SetFont("Editor", "Consolas 10");
                }
                else if (Platform.IsMac)
                {
                    FontService.SetFont("Editor", "Menlo 10");
                }

                Initialize();
            });
        }

        private static void Initialize()
        {
            var container = new ContainerConfiguration()
                .WithAssembly(typeof(MainViewModel).Assembly)   // RoslynPad.Common.UI
                .WithAssembly(typeof(Program).Assembly);        // RoslynPad.Gtk
            var locator = container.CreateContainer().GetExport<IServiceLocator>();

            var viewModel = locator.GetInstance<MainContent>();
            viewModel.Initialize();
        }
    }


    [Export]
    internal class MainContent
    {
        private readonly MainViewModel _viewModel;
        private readonly Dictionary<OpenDocumentViewModel, Document> _documents;

        [ImportingConstructor]
        public MainContent(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            _documents = new Dictionary<OpenDocumentViewModel, Document>();
        }

        public void Initialize()
        {
            _viewModel.OpenDocuments.CollectionChanged += OpenDocumentsOnCollectionChanged;
            _viewModel.Initialize(new[] { typeof(Program).Assembly });
        }

        private async void OpenDocumentsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            var viewModel = (OpenDocumentViewModel)args.NewItems[0];

            if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                Document document;
                if (_documents.TryGetValue(viewModel, out document))
                {
                    IdeApp.Workbench.Documents.Find(x => x == document)?.Close();
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Add)
            {
                var document = await AddDocument(viewModel).ConfigureAwait(true);
                _documents[viewModel] = document;
            }
        }

        private async Task<Document> AddDocument(OpenDocumentViewModel viewModel)
        {
            var document = viewModel.Document == null 
                ? IdeApp.Workbench.NewDocument("X", "text/plain", string.Empty)
                : await IdeApp.Workbench.OpenDocument(new FilePath(viewModel.Document.Path), new Project());

            var editor = document.Editor;
            var host = _viewModel.RoslynHost;

            var textContainer = new MonoDevelopSourceTextContainer(editor);

            await viewModel.Initialize(textContainer, args => { }, o => { },
                () => new TextSpan(editor.SelectionRange.Offset, editor.SelectionRange.Length),
                null).ConfigureAwait(true);

            var documentId = viewModel.DocumentId;

            var options = new CustomEditorOptions(editor.Options)
            {
                ShowLineNumberMargin = false,
                TabsToSpaces = true,
                ShowWhitespaces = ShowWhitespaces.Never
            };

            editor.Options = options;
            var extension = new RoslynCompletionTextEditorExtension(host, documentId);
            editor.SetExtensionChain(document, TextEditorExtension.GetDefaultExtensions().Concat(extension));
            editor.SemanticHighlighting = new RoslynSemanticHighlighting(editor, document, host, documentId);

            return document;
        }
    }
}
