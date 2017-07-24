using System;
using System.ComponentModel;
using System.Composition;
using System.IO;
using Newtonsoft.Json;

namespace RoslynPad.UI
{
    public interface IApplicationSettings : INotifyPropertyChanged
    {
        void LoadFrom(string path);

        bool SendErrors { get; set; }
        string LatestVersion { get; set; }
        string WindowBounds { get; set; }
        string DockLayout { get; set; }
        string WindowState { get; set; }
        double EditorFontSize { get; set; }
        string DocumentPath { get; set; }
        bool SearchFileContents { get; set; }
        bool SearchUsingRegex { get; set; }
    }

    [Export(typeof(IApplicationSettings)), Shared]
    internal class ApplicationSettings : NotificationObject, IApplicationSettings
    {
        private readonly ITelemetryProvider _telemetryProvider;
        private string _path;

        private bool _sendErrors;
        private string _latestVersion;
        private string _windowBounds;
        private string _dockLayout;
        private string _windowState;
        private double _editorFontSize;
        private string _documentPath;
        private bool _searchFileContents;
        private bool _searchUsingRegex;

        [ImportingConstructor]
        public ApplicationSettings(ITelemetryProvider telemetryProvider)
        {
            _telemetryProvider = telemetryProvider;
        }

        public void LoadFrom(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            LoadSettings(path);

            _path = path;
        }

        public bool SendErrors
        {
            get => _sendErrors;
            set => SetProperty(ref _sendErrors, value);
        }

        public string LatestVersion
        {
            get => _latestVersion;
            set => SetProperty(ref _latestVersion, value);
        }

        public string WindowBounds
        {
            get => _windowBounds;
            set => SetProperty(ref _windowBounds, value);
        }

        public string DockLayout
        {
            get => _dockLayout;
            set => SetProperty(ref _dockLayout, value);
        }

        public string WindowState
        {
            get => _windowState;
            set => SetProperty(ref _windowState, value);
        }

        public double EditorFontSize
        {
            get => _editorFontSize;
            set => SetProperty(ref _editorFontSize, value);
        }

        public string DocumentPath
        {
            get => _documentPath;
            set => SetProperty(ref _documentPath, value);
        }

        public bool SearchFileContents
        {
            get => _searchFileContents;
            set => SetProperty(ref _searchFileContents, value);
        }

        public bool SearchUsingRegex
        {
            get => _searchUsingRegex;
            set => SetProperty(ref _searchUsingRegex, value);
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            SaveSettings();
        }

        private void LoadSettings(string path)
        {
            if (!File.Exists(path))
            {
                LoadDefaultSettings();
                return;
            }

            try
            {
                var serializer = new JsonSerializer();
                using (var reader = File.OpenText(path))
                {
                    serializer.Populate(reader, this);
                }
            }
            catch (Exception e)
            {
                LoadDefaultSettings();
                _telemetryProvider.ReportError(e);
            }
        }

        private void LoadDefaultSettings()
        {
            SendErrors = true;
            EditorFontSize = 12;
        }

        private void SaveSettings()
        {
            if (_path == null) return;
            
            try
            {
                var serializer = new JsonSerializer {Formatting = Formatting.Indented};
                using (var writer = File.CreateText(_path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception e)
            {
                _telemetryProvider.ReportError(e);
            }
        }
    }
}