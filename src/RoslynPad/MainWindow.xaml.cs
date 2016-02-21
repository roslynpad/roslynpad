using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using RoslynPad.Editor;
using RoslynPad.Properties;
using RoslynPad.Roslyn;
using RoslynPad.Runtime;

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
                if (mode == DumpMode.PropertyGrid)
                {
                    ThePropertyGrid.SelectedObject = o;
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

        private void OnPlayCommand(object sender, RoutedEventArgs e)
        {
            _objects.Clear();

            try
            {
                _interactiveManager.Execute();
            }
            catch (Exception ex)
            {
                _objects.Add(new ResultObject(ex));
            }
        }
    }

    class ResultObject
    {
        private readonly object _o;
        private readonly PropertyDescriptor _property;
        private bool _initialized;
        private string _header;
        private IEnumerable _children;

        public ResultObject(object o, PropertyDescriptor property = null)
        {
            _o = o;
            _property = property;
        }

        public string Header
        {
            get
            {
                Initialize();
                return _header;
            }
        }

        public IEnumerable Children
        {
            get
            {
                Initialize();
                return _children;
            }
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_o == null)
            {
                _header = "<null>";
                return;
            }

            if (_property != null)
            {
                var value = _property.GetValue(_o);
                _header = _property.Name + " = " + value;
                _children = new[] { new ResultObject(value)  };
                return;
            }

            var s = _o as string;
            if (s != null)
            {
                _header = s;
                return;
            }

            var e = _o as IEnumerable;
            if (e != null)
            {
                var enumerableChildren = e.Cast<object>().Select(x => new ResultObject(x)).ToArray();
                _children = enumerableChildren;
                _header = $"<enumerable count={enumerableChildren.Length}>";
                return;
            }

            var properties = TypeDescriptor.GetProperties(_o).Cast<PropertyDescriptor>()
                .Select(p => new ResultObject(_o, p)).ToArray();
            _header = _o.ToString();
            if (properties.Length > 0)
            {
                _children = properties;
            }
        }
    }
}
