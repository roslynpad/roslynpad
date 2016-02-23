using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using RoslynPad.Editor;
using RoslynPad.Properties;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.Runtime;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string DefaultSessionText = @"Enumerable.Range(0, 100).Select(t => new { M = t }.DumpToPropertyGrid()).Dump();";

        private readonly object _lock;
        private readonly ObservableCollection<ResultObject> _objects;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly InteractiveManager _interactiveManager;
        private readonly TextMarkerService _textMarkerService;

        public MainWindow()
        {
            InitializeComponent();

            _textMarkerService = new TextMarkerService(Editor);
            Editor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            Editor.TextArea.TextView.LineTransformers.Add(_textMarkerService);

            ConfigureEditor();

            _lock = new object();
            _objects = new ObservableCollection<ResultObject>();
            BindingOperations.EnableCollectionSynchronization(_objects, _objects);
            Results.ItemsSource = _objects;

            ObjectExtensions.Dumped += (o, mode) =>
            {
                if (mode == DumpTarget.PropertyGrid)
                {
                    ThePropertyGrid.SelectedObject = o;

                    foreach (var prop in ThePropertyGrid.Properties.OfType<PropertyItem>())
                    {
                        var propertyType = prop.PropertyType;
                        if (!propertyType.IsPrimitive && propertyType != typeof(string))
                        {
                            prop.IsExpandable = true;
                        }
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        _objects.Add(new ResultObject(o));
                    }
                }
            };

            var syncContext = SynchronizationContext.Current;

            _interactiveManager = new InteractiveManager();
            _interactiveManager.DiagnosticsUpdated += (sender, args) => syncContext.Post(o => ProcessDiagnostics(args), null);
            _interactiveManager.SetDocument(Editor.AsTextContainer());

            Editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_interactiveManager);
        }

        private void ProcessDiagnostics(DiagnosticsUpdatedArgs args)
        {
            //lock (_lock)
            //{
            //    _objects.Add(new ResultObject(args));
            //}

            _textMarkerService.RemoveAll(x => true);

            foreach (var diagnosticData in args.Diagnostics)
            {
                if (diagnosticData.Severity == DiagnosticSeverity.Hidden || diagnosticData.IsSuppressed)
                {
                    continue;
                }

                var marker = _textMarkerService.Create(diagnosticData.TextSpan.Start, diagnosticData.TextSpan.Length);
                marker.MarkerColor = GetDiagnosticsColor(diagnosticData);
                marker.ToolTip = diagnosticData.Message;
            }
        }

        private static Color GetDiagnosticsColor(DiagnosticData diagnosticData)
        {
            switch (diagnosticData.Severity)
            {
                case DiagnosticSeverity.Info:
                    return Colors.LimeGreen;
                case DiagnosticSeverity.Warning:
                    return Colors.DodgerBlue;
                case DiagnosticSeverity.Error:
                    return Colors.Red;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ConfigureEditor()
        {
            Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
            var lastSessionText = Settings.Default.LastSessionText;
            Editor.Text = string.IsNullOrEmpty(lastSessionText) ? DefaultSessionText : lastSessionText;
            Application.Current.Exit += (sender, args) =>
            {
                Settings.Default.LastSessionText = Editor.Text;
                Settings.Default.Save();
            };
        }

        private async void OnPlayCommand(object sender, RoutedEventArgs e)
        {
            lock (_lock)
            {
                _objects.Clear();
            }

            try
            {
                await _interactiveManager.Execute().ConfigureAwait(true);
            }
            catch (CompilationErrorException ex)
            {
                lock (_lock)
                {
                    foreach (var diagnostic in ex.Diagnostics)
                    {
                        _objects.Add(new ResultObject(diagnostic));
                    }
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _objects.Add(new ResultObject(ex));
                }
            }
        }
    }
}
