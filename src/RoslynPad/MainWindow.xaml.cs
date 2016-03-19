using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using NuGet;
using RoslynPad.Editor;
using RoslynPad.Roslyn.Diagnostics;
using RoslynPad.RoslynEditor;
using RoslynPad.Runtime;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Settings = RoslynPad.Properties.Settings;

namespace RoslynPad
{
    public partial class MainWindow
    {
        private const string DefaultSessionText = @"Enumerable.Range(0, 100).Select(t => new { M = t }.DumpToPropertyGrid()).Dump();";

        private readonly object _lock;
        private readonly ObservableCollection<ResultObject> _objects;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly TextMarkerService _textMarkerService;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly ContextActionsRenderer _contextActionsRenderer;
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            _viewModel = new MainViewModel();
            _viewModel.NuGet.PackageInstalled += NuGetOnPackageInstalled;
            DataContext = _viewModel;

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

            _viewModel.RoslynHost.GetService<IDiagnosticService>().DiagnosticsUpdated += (sender, args) => syncContext.Post(o => ProcessDiagnostics(args), null);
            var avalonEditTextContainer = new AvalonEditTextContainer(Editor);
            _viewModel.RoslynHost.SetDocument(avalonEditTextContainer);
            _viewModel.RoslynHost.ApplyingTextChange += (id, text) => avalonEditTextContainer.UpdateText(text);

            Editor.TextArea.TextView.LineTransformers.Insert(0, new RoslynHighlightingColorizer(_viewModel.RoslynHost));

            _contextActionsRenderer = new ContextActionsRenderer(Editor, _textMarkerService);
            _contextActionsRenderer.Providers.Add(new RoslynContextActionProvider(_viewModel.RoslynHost));

            Editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_viewModel.RoslynHost);
        }

        private void NuGetOnPackageInstalled(IPackage package, NuGetInstallResult installResult)
        {
            if (installResult.References.Count == 0) return;

            var text = string.Join(Environment.NewLine,
                installResult.References.Select(r => Path.Combine(MainViewModel.NuGetPathVariableName, r))
                .Concat(installResult.FrameworkReferences)
                .Where(r => !_viewModel.RoslynHost.HasReference(r))
                .Select(r => "#r \"" + r + "\"")) + Environment.NewLine;

            Dispatcher.InvokeAsync(() => Editor.Document.Insert(0, text, AnchorMovementType.Default));
        }

        private void OnDumped(object o, DumpTarget mode)
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
                AddResult(o);
            }
        }

        private void AddResult(object o)
        {
            lock (_lock)
            {
                _objects.Add(ResultObject.Create(o));
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

                var marker = _textMarkerService.TryCreate(diagnosticData.TextSpan.Start, diagnosticData.TextSpan.Length);
                if (marker != null)
                {
                    marker.MarkerColor = GetDiagnosticsColor(diagnosticData);
                    marker.ToolTip = diagnosticData.Message;
                }
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
            //Editor.SyntaxHighlighting = GetSyntaxHighlighting();
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
                var result = await _viewModel.RoslynHost.Execute().ConfigureAwait(true);
                if (result != null)
                {
                    AddResult(result);
                }
            }
            catch (CompilationErrorException ex)
            {
                lock (_lock)
                {
                    foreach (var diagnostic in ex.Diagnostics)
                    {
                        _objects.Add(ResultObject.Create(diagnostic));
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult(ex);
            }
        }

        private void Editor_OnKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.R && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            //{
            //    _roslynHost.GetService<IInlineRenameService>().StartInlineSession(
            //        _roslynHost.CurrentDocument, new TextSpan(Editor.CaretOffset, 1));
            //}
        }
    }
}
