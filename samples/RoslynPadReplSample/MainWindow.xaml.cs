using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Shared.Utilities;


namespace RoslynPadReplSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<DocumentViewModel> _documents;
        private RoslynHost _host;
        private DocumentationProviderService _documentationProviderService;

        public MainWindow()
        {
            InitializeComponent();

            _documents = new ObservableCollection<DocumentViewModel>();
            Items.ItemsSource = _documents;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            _documentationProviderService = new();

            var objectLocation = typeof(object).Assembly.Location;
            var regexLocation = typeof(System.Text.RegularExpressions.Regex).Assembly.Location;
            var enumerableLocation = typeof(System.Linq.Enumerable).Assembly.Location;

            _host = new RoslynHost(additionalAssemblies: new[]
            {
                Assembly.Load("RoslynPad.Roslyn.Windows"),
                Assembly.Load("RoslynPad.Editor.Windows")
            }, RoslynHostReferences.NamespaceDefault.With(new[]
            {
                MetadataReference.CreateFromFile(objectLocation, documentation:_documentationProviderService.GetDocumentationProvider(objectLocation)),
                MetadataReference.CreateFromFile(regexLocation, documentation:_documentationProviderService.GetDocumentationProvider(regexLocation)),
                MetadataReference.CreateFromFile(enumerableLocation, documentation:_documentationProviderService.GetDocumentationProvider(enumerableLocation)),
            }));

            AddNewDocument();
        }

        private void AddNewDocument(DocumentViewModel previous = null)
        {
            _documents.Add(new DocumentViewModel(_host, previous));
        }

        private void OnItemLoaded(object sender, EventArgs e)
        {
            var editor = (RoslynCodeEditor)sender;
            editor.Loaded -= OnItemLoaded;
            editor.Focus();

            var viewModel = (DocumentViewModel)editor.DataContext;
            var workingDirectory = Directory.GetCurrentDirectory();

            var previous = viewModel.LastGoodPrevious;
            if (previous != null)
            {
                editor.CreatingDocument += (o, args) =>
                {
                    args.DocumentId = _host.AddRelatedDocument(previous.Id, new DocumentCreationArgs(
                        args.TextContainer, workingDirectory, args.ProcessDiagnostics,
                        args.TextContainer.UpdateText));
                };
            }

            var documentId = editor.Initialize(_host, new ClassificationHighlightColors(),
                workingDirectory, string.Empty);

            viewModel.Initialize(documentId);
        }

        private async void OnEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var editor = (RoslynCodeEditor)sender;
                if (editor.IsCompletionWindowOpen)
                {
                    return;
                }

                e.Handled = true;

                var viewModel = (DocumentViewModel)editor.DataContext;
                if (viewModel.IsReadOnly) return;

                viewModel.Text = editor.Text;
                if (await viewModel.TrySubmit())
                {
                    AddNewDocument(viewModel);
                }
            }
        }

        class DocumentViewModel : INotifyPropertyChanged
        {
            private bool _isReadOnly;
            private readonly RoslynHost _host;
            private string _result;

            public DocumentViewModel(RoslynHost host, DocumentViewModel previous)
            {
                _host = host;
                Previous = previous;
            }

            internal void Initialize(DocumentId id)
            {
                Id = id;
            }


            public DocumentId Id { get; private set; }

            public bool IsReadOnly
            {
                get { return _isReadOnly; }
                private set { SetProperty(ref _isReadOnly, value); }
            }

            public DocumentViewModel Previous { get; }

            public DocumentViewModel LastGoodPrevious
            {
                get
                {
                    var previous = Previous;

                    while (previous != null && previous.HasError)
                    {
                        previous = previous.Previous;
                    }

                    return previous;
                }
            }

            public Script<object> Script { get; private set; }

            public string Text { get; set; }

            public bool HasError { get; private set; }

            public string Result
            {
                get { return _result; }
                private set { SetProperty(ref _result, value); }
            }

            private static MethodInfo HasSubmissionResult { get; } =
                typeof(Compilation).GetMethod(nameof(HasSubmissionResult), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            private static PrintOptions PrintOptions { get; } = 
                new PrintOptions { MemberDisplayFormat = MemberDisplayFormat.SeparateLines };

            public async Task<bool> TrySubmit()
            {
                Result = null;

                Script = LastGoodPrevious?.Script.ContinueWith(Text) ??
                    CSharpScript.Create(Text, ScriptOptions.Default
                        .WithReferences(_host.DefaultReferences)
                        .WithImports(_host.DefaultImports));

                var compilation = Script.GetCompilation();
                var hasResult = (bool)HasSubmissionResult.Invoke(compilation, null);
                var diagnostics = Script.Compile();
                if (diagnostics.Any(t => t.Severity == DiagnosticSeverity.Error))
                {
                    Result = string.Join(Environment.NewLine, diagnostics.Select(FormatObject));
                    return false;
                }

                IsReadOnly = true;

                await Execute(hasResult);

                return true;
            }

            private async Task Execute(bool hasResult)
            {
                try
                {
                    var result = await Script.RunAsync();

                    if (result.Exception != null)
                    {
                        HasError = true;
                        Result = FormatException(result.Exception);
                    }
                    else
                    {
                        Result = hasResult ? FormatObject(result.ReturnValue) : null;
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;
                    Result = FormatException(ex);
                }
            }

            private static string FormatException(Exception ex)
            {
                return CSharpObjectFormatter.Instance.FormatException(ex);
            }

            private static string FormatObject(object o)
            {
                return CSharpObjectFormatter.Instance.FormatObject(o, PrintOptions);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
                if (!EqualityComparer<T>.Default.Equals(field, value))
                {
                    field = value;
                    OnPropertyChanged(propertyName);
                    return true;
                }
                return false;
            }

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        sealed class DocumentationProviderService : IDocumentationProviderService
        {
            #region Fields

            private static readonly Lazy<(string assemblyPath, string docPath)> _referenceAssembliesPath =
                new(GetReferenceAssembliesPath);

            private readonly ConcurrentDictionary<string, DocumentationProvider> _assemblyPathToDocumentationProviderMap
                = new();
            #endregion

            #region Properties

            public static (string assemblyPath, string docPath) ReferenceAssembliesPath => _referenceAssembliesPath.Value;
            #endregion

            #region Public Methods

            public DocumentationProvider GetDocumentationProvider(string location)
            {
                string finalPath = Path.ChangeExtension(location, "xml");

                return _assemblyPathToDocumentationProviderMap.GetOrAdd(location,
                    _ =>
                    {
                        if (!File.Exists(finalPath))
                            finalPath = GetFilePath(ReferenceAssembliesPath.docPath, finalPath) ??
                            GetFilePath(ReferenceAssembliesPath.assemblyPath, finalPath);

                        return finalPath == null ? null : XmlDocumentationProvider.CreateFromFile(finalPath);
                    });
            }
            #endregion

            #region Private Methods

            private static string GetFilePath(string path, string location)
            {
                if (path != null)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var referenceLocation = Path.Combine(path, Path.GetFileName(location));
                    if (File.Exists(referenceLocation))
                        return referenceLocation;
                }

                return null;
            }

            private static (string assemblyPath, string docPath) GetReferenceAssembliesPath()
            {
                string assemblyPath = null;
                string docPath = null;

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    // all NuGet
                    return (assemblyPath, docPath);

                var programFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");

                if (string.IsNullOrEmpty(programFiles))
                    programFiles = Environment.GetEnvironmentVariable("ProgramFiles");

                if (string.IsNullOrEmpty(programFiles))
                    return (assemblyPath, docPath);

                var path = Path.Combine(programFiles, @"Reference Assemblies\Microsoft\Framework\.NETFramework");
                if (Directory.Exists(path))
                {
                    assemblyPath = IOUtilities.PerformIO(() => Directory.GetDirectories(path), Array.Empty<string>())
                        .Select(x => new { path = x, version = GetFxVersionFromPath(x) })
                        .OrderByDescending(x => x.version)
                        .FirstOrDefault(x => File.Exists(Path.Combine(x.path, "System.dll")))?.path;

                    if (assemblyPath == null || !File.Exists(Path.Combine(assemblyPath, "System.xml")))
                        docPath = GetReferenceDocumentationPath(path);
                }

                return (assemblyPath, docPath);
            }

            private static string GetReferenceDocumentationPath(string path)
            {
                string docPath = null;

                var docPathTemp = Path.Combine(path, "V4.X");
                if (File.Exists(Path.Combine(docPathTemp, "System.xml")))
                    docPath = docPathTemp;
                else
                {
                    var localeDirectory = IOUtilities.PerformIO(() => Directory.GetDirectories(docPathTemp),
                        Array.Empty<string>()).FirstOrDefault();
                    if (localeDirectory != null && File.Exists(Path.Combine(localeDirectory, "System.xml")))
                        docPath = localeDirectory;
                }

                return docPath;
            }

            private static Version GetFxVersionFromPath(string path)
            {
                var name = Path.GetFileName(path);
                if (name?.StartsWith("v", StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (Version.TryParse(name.Substring(1), out var version))
                        return version;
                }

                return new Version(0, 0);
            }
            #endregion
        }
    }
}
