using System;
using System.ComponentModel;
using System.Composition;
using System.Reflection;
using System.Runtime.CompilerServices;
using RoslynPad.Properties;

namespace RoslynPad.UI
{
    public interface IApplicationSettings : INotifyPropertyChanged
    {
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

        [ImportingConstructor]
        public ApplicationSettings(ITelemetryProvider telemetryProvider)
        {
            _telemetryProvider = telemetryProvider;
        }

        [DefaultValue(true)]
        public bool SendErrors { get => GetValue<bool>(); set => SetValue(value); }
        public string LatestVersion { get => GetValue<string>(); set => SetValue(value); }
        public string WindowBounds { get => GetValue<string>(); set => SetValue(value); }
        public string DockLayout { get => GetValue<string>(); set => SetValue(value); }
        public string WindowState { get => GetValue<string>(); set => SetValue(value); }
        public double EditorFontSize { get => GetValue<double>(); set => SetValue(value); }
        public string DocumentPath { get => GetValue<string>(); set => SetValue(value); }
        public bool SearchFileContents { get => GetValue<bool>(); set => SetValue(value); }
        public bool SearchUsingRegex { get => GetValue<bool>(); set => SetValue(value); }

        private T GetValue<T>([CallerMemberName] string propertyName = null)
        {
            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                return (T)Settings.Default[propertyName];
            }
            catch (Exception e)
            {
                _telemetryProvider.ReportError(e);
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            var defaultValueAttribute = typeof(ApplicationSettings).GetProperty(propertyName)
                .GetCustomAttribute<DefaultValueAttribute>();
            return defaultValueAttribute != null ? (T)defaultValueAttribute.Value : default(T);
        }

        private void SetValue<T>(T value, [CallerMemberName] string propertyName = null)
        {
            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                Settings.Default[propertyName] = value;
                Settings.Default.Save();
                OnPropertyChanged(propertyName);
            }
            catch (Exception ex)
            {
                _telemetryProvider.ReportError(ex);
            }
        }
    }
}