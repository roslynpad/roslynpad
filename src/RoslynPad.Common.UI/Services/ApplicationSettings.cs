using System.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Versioning;
using RoslynPad.Themes;

namespace RoslynPad.UI;

[Export(typeof(IApplicationSettings)), Shared]
internal class ApplicationSettings : IApplicationSettings
{
    private const string DefaultConfigFileName = "RoslynPad.json";

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly IErrorReporter? _errorReporter;
    private readonly IKeyBindingService _keyBindingService;
    private SerializableValues _values;
    private string? _path;

    [ImportingConstructor]
    public ApplicationSettings(
        IKeyBindingService keyBindingService,
        [Import(AllowDefault = true)] IErrorReporter errorReporter)
    {
        _keyBindingService = keyBindingService;
        _errorReporter = errorReporter;
        _values = new SerializableValues();
        InitializeValues();

        // Initialize static accessor for use in XAML converters
        KeyBindings.Service = keyBindingService;
    }

    private void InitializeValues()
    {
        _values.PropertyChanged += (_, _) => SaveSettings();
        _values.Settings = this;
    }

    public void LoadDefault() =>
        LoadFrom(Path.Combine(GetDefaultDocumentPath(), DefaultConfigFileName));

    public void LoadFrom(string path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

        LoadSettings(path);

        _path = path;
    }

    public IApplicationSettingsValues Values => _values;

    public string GetDefaultDocumentPath()
    {
        string? documentsPath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        else // Unix or Mac
        {
            documentsPath = Environment.GetEnvironmentVariable("HOME");
        }

        if (string.IsNullOrEmpty(documentsPath))
        {
            documentsPath = "/";
            _errorReporter?.ReportError(new InvalidOperationException("Unable to locate the user documents folder; Using root"));
        }

        return Path.Combine(documentsPath, "RoslynPad");
    }

    private void LoadSettings(string path)
    {
        if (!File.Exists(path))
        {
            _values.LoadDefaultSettings();
            _keyBindingService.LoadOverrides(_values);
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            _values = JsonSerializer.Deserialize<SerializableValues>(json, s_serializerOptions) ?? new SerializableValues();
            _values.LoadMissingDefaults();
            InitializeValues();
        }
        catch (Exception e)
        {
            _values.LoadDefaultSettings();
            _errorReporter?.ReportError(e);
        }

        _keyBindingService.LoadOverrides(_values);
    }

    private void SaveSettings()
    {
        if (_path == null) return;

        try
        {
            using var stream = File.Create(_path);
            JsonSerializer.Serialize(stream, _values, s_serializerOptions);
        }
        catch (Exception e)
        {
            _errorReporter?.ReportError(e);
        }
    }

    private class SerializableValues : NotificationObject, IApplicationSettingsValues
    {
        private const int LiveModeDelayMsDefault = 2000;
        private const int DefaultFontSize = 12;

        public void LoadDefaultSettings()
        {
            FormatDocumentOnComment = true;
            EditorFontSize = DefaultFontSize;
            OutputFontSize = DefaultFontSize;
            LiveModeDelayMs = LiveModeDelayMsDefault;
            EditorFontFamily = GetDefaultPlatformFontFamily();
            DefaultUsings = GetDefaultUsings();
        }

        public void LoadMissingDefaults()
        {
            if (EditorFontSize <= 0)
            {
                EditorFontSize = DefaultFontSize;
            }

            if (OutputFontSize <= 0)
            {
                OutputFontSize = DefaultFontSize;
            }

            if (LiveModeDelayMs <= 0)
            {
                LiveModeDelayMs = LiveModeDelayMsDefault;
            }

            if (string.IsNullOrWhiteSpace(EditorFontFamily))
            {
                EditorFontFamily = GetDefaultPlatformFontFamily();
            }

            if (DefaultUsings is null || DefaultUsings.Length == 0)
            {
                DefaultUsings = GetDefaultUsings();
            }
        }

        private bool SetProperty(ref string? field, string? value, [CallerMemberName] string? propertyName = null) =>
            base.SetProperty(ref field, string.IsNullOrWhiteSpace(value) ? null : value.Trim(), propertyName);

        private static string? NormalizeDefaultPlatformName(string? value)
        {
            value = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (value is null)
            {
                return null;
            }

            if (NuGetVersion.TryParse(value, out var version))
            {
                return version.ToNormalizedString();
            }

            var lastSpaceIndex = value.LastIndexOf(' ');
            if (lastSpaceIndex >= 0 && NuGetVersion.TryParse(value[(lastSpaceIndex + 1)..], out version))
            {
                return version.ToNormalizedString();
            }

            return value;
        }

        internal static string[] GetDefaultUsings() => [
            "System",
            "System.Threading",
            "System.Threading.Tasks",
            "System.Collections",
            "System.Collections.Generic",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Linq",
            "System.IO",
            "System.Reflection",
            "RoslynPad.Runtime",
        ];

        private static string GetDefaultPlatformFontFamily()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Cascadia Code,Consolas";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "Menlo";
            }
            else
            {
                return "Monospace";
            }
        }

        public IList<KeyBinding>? KeyBindings
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string? LatestVersion
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string? WindowBounds
        {
            get;
            set => SetProperty(ref field, value);
        }

        [JsonPropertyName("dockLayoutV2")]
        public string? DockLayout
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string? WindowState
        {
            get;
            set => SetProperty(ref field, value);
        }

        public double EditorFontSize
        {
            get;
            set => SetProperty(ref field, Math.Clamp(value, 8, 72));
        } = DefaultFontSize;

        private string _editorFontFamily = GetDefaultPlatformFontFamily();

        public string EditorFontFamily
        {
            get => _editorFontFamily;
            set => base.SetProperty(ref _editorFontFamily, string.IsNullOrWhiteSpace(value) ? GetDefaultPlatformFontFamily() : value.Trim());
        }

        public double OutputFontSize
        {
            get;
            set => SetProperty(ref field, Math.Clamp(value, 8, 72));
        } = DefaultFontSize;

        public string? DocumentPath
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool SearchFileContents
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool SearchUsingRegex
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool OptimizeCompilation
        {
            get;
            set => SetProperty(ref field, value);
        }

        public int LiveModeDelayMs
        {
            get;
            set => SetProperty(ref field, Math.Max(0, value));
        } = LiveModeDelayMsDefault;

        public bool SearchWhileTyping
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string? SdkLocation
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string? DefaultPlatformName
        {
            get;
            set => SetProperty(ref field, NormalizeDefaultPlatformName(value));
        }

        public double? WindowFontSize
        {
            get;
            set => SetProperty(ref field, value is > 0 ? value : null);
        }

        public bool FormatDocumentOnComment
        {
            get;
            set => SetProperty(ref field, value);
        } = true;

        public string? CustomThemePath
        {
            get;
            set => SetProperty(ref field, value);
        }

        public ThemeType? CustomThemeType
        {
            get;
            set => SetProperty(ref field, value);
        }

        public BuiltInTheme BuiltInTheme
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string[]? DefaultUsings
        {
            get;
            set => SetProperty(ref field, value);
        } = GetDefaultUsings();

        [JsonIgnore]
        public string EffectiveDocumentPath
        {
            get
            {
                if (field == null)
                {

                    var userDefinedPath = DocumentPath;
                    field = !string.IsNullOrEmpty(userDefinedPath) && Directory.Exists(userDefinedPath)
                        ? userDefinedPath!
                        : Settings?.GetDefaultDocumentPath() ?? string.Empty;
                }

                return field;
            }
        }

        [JsonIgnore]
        public ApplicationSettings? Settings { get; set; }
    }
}
