using System;
using System.ComponentModel;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Avalon.Windows.Controls;
using Microsoft.Practices.ServiceLocation;
using RoslynPad.UI;
using RoslynPad.Utilities;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace RoslynPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MainViewModel _viewModel;
        private bool _isClosing;
        private bool _isClosed;

        internal MainWindow()
        {
            Loaded += OnLoaded;

            var container = new ContainerConfiguration()
                .WithAssembly(typeof(MainViewModel).Assembly)   // RoslynPad.Common.UI
                .WithAssembly(typeof(MainWindow).Assembly);     // RoslynPad
            var locator = container.CreateContainer().GetExport<IServiceLocator>();

            _viewModel = locator.GetInstance<MainViewModel>();

            DataContext = _viewModel;
            InitializeComponent();
            DocumentsPane.ToggleAutoHide();

            LoadWindowLayout();
            LoadDockLayout();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            _viewModel.Initialize();
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!_isClosing)
            {
                SaveDockLayout();
                SaveWindowLayout();
                
                _isClosing = true;
                IsEnabled = false;
                e.Cancel = true;

                try
                {
                    await Task.Run(() => _viewModel.OnExit()).ConfigureAwait(true);
                }
                catch
                {
                    // ignored
                }

                _isClosed = true;
                // ReSharper disable once UnusedVariable
                var closeTask = Dispatcher.InvokeAsync(Close);
            }
            else
            {
                e.Cancel = !_isClosed;
            }
        }

        private void LoadWindowLayout()
        {
            var boundsString = _viewModel.Settings.WindowBounds;
            if (!string.IsNullOrEmpty(boundsString))
            {
                try
                {
                    var bounds = Rect.Parse(boundsString);
                    if (bounds != default(Rect))
                    {
                        Left = bounds.Left;
                        Top = bounds.Top;
                        Width = bounds.Width;
                        Height = bounds.Height;
                    }
                }
                catch (FormatException)
                {
                }
            }

            if (Enum.TryParse(_viewModel.Settings.WindowState, out WindowState state) &&
                state != WindowState.Minimized)
            {
                WindowState = state;
            }
        }

        private void SaveWindowLayout()
        {
            _viewModel.Settings.WindowBounds = RestoreBounds.ToString(CultureInfo.InvariantCulture);
            _viewModel.Settings.WindowState = WindowState.ToString();
        }

        private void LoadDockLayout()
        {
            var layout = _viewModel.Settings.DockLayout;
            if (string.IsNullOrEmpty(layout)) return;

            var serializer = new XmlLayoutSerializer(DockingManager);
            var reader = new StringReader(layout);
            try
            {
                serializer.Deserialize(reader);
            }
            catch
            {
                // ignored
            }
        }

        private void SaveDockLayout()
        {
            var serializer = new XmlLayoutSerializer(DockingManager);
            var document = new XDocument();
            using (var writer = document.CreateWriter())
            {
                serializer.Serialize(writer);
            }
            document.Root?.Element("FloatingWindows")?.Remove();
            _viewModel.Settings.DockLayout = document.ToString();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            Application.Current.Shutdown();
        }
        
        private async void DockingManager_OnDocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            e.Cancel = true;
            var document = (OpenDocumentViewModel)e.Document.Content;
            await _viewModel.CloseDocument(document).ConfigureAwait(false);
        }

        private void ViewErrorDetails_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.HasError) return;

            TaskDialog.ShowInline(this, "Unhandled Exception",
                _viewModel.LastError.ToAsyncString(), string.Empty, TaskDialogButtons.Close);
        }

        private void ViewUpdateClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() => Process.Start("https://roslynpad.net/"));
        }
    }
}