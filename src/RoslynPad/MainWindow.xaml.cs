using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis.Scripting;
using RoslynPad.Editor;
using RoslynPad.Properties;
using RoslynPad.Roslyn;
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

        private readonly ObservableCollection<ResultObject> _objects;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly InteractiveManager _interactiveManager;

        public MainWindow()
        {
            InitializeComponent();

            ConfigureEditor();

            _objects = new ObservableCollection<ResultObject>();
            Results.ItemsSource = _objects;

            ObjectExtensions.Dumped += (o, mode) =>
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
                    _objects.Add(new ResultObject(o));
                }
            };

            _interactiveManager = new InteractiveManager();
            _interactiveManager.SetDocument(Editor.AsTextContainer());
            Editor.CompletionProvider = new RoslynCodeEditorCompletionProvider(_interactiveManager);
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
            _objects.Clear();

            try
            {
                await _interactiveManager.Execute().ConfigureAwait(true);
            }
            catch (CompilationErrorException ex)
            {
                foreach (var diagnostic in ex.Diagnostics)
                {
                    _objects.Add(new ResultObject(diagnostic));
                }
            }
            catch (Exception ex)
            {
                _objects.Add(new ResultObject(ex));
            }
        }
    }
}
