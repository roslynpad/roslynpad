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
using RoslynPad.RoslynEditor;
using RoslynPad.Runtime;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace RoslynPad
{
    public partial class MainWindow
    {
        private const string DefaultSessionText = @"Enumerable.Range(0, 100).Select(t => new { M = t }.DumpToPropertyGrid()).Dump();";

        private readonly object _lock;
        private readonly ObservableCollection<ResultObject> _objects;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly RoslynHost _roslynHost;
        private readonly TextMarkerService _textMarkerService;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ContextActionsRenderer _contextActionsRenderer;

        public MainWindow()
        {
            InitializeComponent();

            _textMarkerService = new TextMarkerService(Editor);
            Editor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
            Editor.TextArea.TextView.LineTransformers.Add(_textMarkerService);

            ConfigureEditor();

            _lock = new object();
            _objects = new ObservableCollection<ResultObject>();
            BindingOperations.EnableCollectionSynchronization(_objects, _lock);
            Results.ItemsSource = _objects;

            ObjectExtensions.Dumped += OnDumped;

            var syncContext = SynchronizationContext.Current;

            _roslynHost = new RoslynHost();
            _roslynHost.DiagnosticsUpdated += (sender, args) => syncContext.Post(o => ProcessDiagnostics(args), null);
            var avalonEditTextContainer = new AvalonEditTextContainer(Editor);
            _roslynHost.SetDocument(avalonEditTextContainer);
            _roslynHost.ApplyingTextChange += (id, text) => avalonEditTextContainer.UpdateText(text);

            _contextActionsRenderer = new ContextActionsRenderer(Editor, _textMarkerService);
            _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_roslynHost));

            Editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_roslynHost);
        }

        private void OnDumped(object o, DumpTarget mode)
        {
            if (mode == DumpTarget.PropertyGrid)
            {
                ThePropertyGrid.SelectedObject = o;

                foreach (var prop in ThePropertyGrid.Properties.OfType<PropertyItem>())
                {
                    var propertyType = prop.PropertyType;
                    if (!propertyType.IsPrimitive && propertyType != typeof (string))
                    {
                        prop.IsExpandable = true;
                    }
                }
            }
            else
            {
                AddResult(o);
            }
        }

        private void AddResult(object o)
        {
            lock (_lock)
            {
                _objects.Add(new ResultObject(o));
            }
        }

        private void ProcessDiagnostics(DiagnosticsUpdatedArgs args)
        {
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
                await _roslynHost.Execute().ConfigureAwait(true);
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
                AddResult(ex);
            }
        }
    }
}
