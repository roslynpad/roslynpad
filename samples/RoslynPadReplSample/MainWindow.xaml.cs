using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace RoslynPadReplSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<DocumentViewModel> _documents;
        private RoslynHost _host;

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

            _host = new RoslynHost(additionalAssemblies: new[]
            {
                Assembly.Load("RoslynPad.Roslyn.Windows"),
                Assembly.Load("RoslynPad.Editor.Windows")
            }, RoslynHostReferences.NamespaceDefault.With(new[]
            { 
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
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
                    // ReSharper disable once ExplicitCallerInfoArgument
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
    }
}
