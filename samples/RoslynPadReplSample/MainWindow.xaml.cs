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
            });

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

            editor.CreatingDocument += (o, args) =>
            {
                if (viewModel.Previous != null)
                {
                    args.DocumentId = _host.AddRelatedDocument(viewModel.Previous.Id,
                        args.TextContainer, workingDirectory, args.ProcessDiagnostics,
                        args.TextContainer.UpdateText);
                }
            };

            var documentId = editor.Initialize(_host, new ClassificationHighlightColors(),
                workingDirectory, string.Empty);

            viewModel.Initialize(documentId);
        }

        private async void OnEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                var editor = (RoslynCodeEditor)sender;
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

            public DocumentViewModel Previous { get; }
            public DocumentId Id { get; private set; }

            public DocumentViewModel(RoslynHost host, DocumentViewModel previous)
            {
                _host = host;
                Previous = previous;
            }

            internal void Initialize(DocumentId id)
            {
                Id = id;
            }

            public bool IsReadOnly
            {
                get { return _isReadOnly; }
                private set { SetProperty(ref _isReadOnly, value); }
            }

            public Script<object> Script { get; private set; }

            public string Text { get; set; }

            public string Result
            {
                get { return _result; }
                private set { SetProperty(ref _result, value); }
            }

            public async Task<bool> TrySubmit()
            {
                Result = null;

                Script = Previous?.Script.ContinueWith(Text) ??
                    CSharpScript.Create(Text, ScriptOptions.Default
                        .WithReferences(_host.DefaultReferences)
                        .WithImports(_host.DefaultImports));

                var diagnostics = Script.Compile();
                if (diagnostics.Any(t => t.Severity == DiagnosticSeverity.Error))
                {
                    Result = string.Join(Environment.NewLine,
                            diagnostics.Select(CSharpObjectFormatter.Instance.FormatObject));
                    return false;
                }

                IsReadOnly = true;

                await Execute();

                return true;
            }

            private async Task Execute()
            {
                try
                {
                    var result = await Script.RunAsync();

                    Result = result.Exception != null
                        ? CSharpObjectFormatter.Instance.FormatException(result.Exception)
                        : CSharpObjectFormatter.Instance.FormatObject(result.ReturnValue);
                }
                catch (Exception ex)
                {
                    Result = CSharpObjectFormatter.Instance.FormatException(ex);
                }
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
