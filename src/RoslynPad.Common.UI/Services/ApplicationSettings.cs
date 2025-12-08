using System.Composition;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    private readonly ITelemetryProvider? _telemetryProvider;
    private readonly IKeyBindingService _keyBindingService;
    private SerializableValues _values;
    private string? _path;

    [ImportingConstructor]
    public ApplicationSettings(
        IKeyBindingService keyBindingService,
        [Import(AllowDefault = true)] ITelemetryProvider telemetryProvider)
    {
        _keyBindingService = keyBindingService;
        _telemetryProvider = telemetryProvider;
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
            _telemetryProvider?.ReportError(new InvalidOperationException("Unable to locate the user documents folder; Using root"));
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
            InitializeValues();
        }
        catch (Exception e)
        {
            _values.LoadDefaultSettings();
            _telemetryProvider?.ReportError(e);
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
            _telemetryProvider?.ReportError(e);
        }
    }

    private class SerializableValues : NotificationObject, IApplicationSettingsValues
    {
        private const int LiveModeDelayMsDefault = 2000;
        private const int DefaultFontSize = 12;

        public void LoadDefaultSettings()
        {
            SendErrors = true;
            FormatDocumentOnComment = true;
            EditorFontSize = DefaultFontSize;
            OutputFontSize = DefaultFontSize;
            LiveModeDelayMs = LiveModeDelayMsDefault;
            EditorFontFamily = GetDefaultPlatformFontFamily();
            DefaultUsings = GetDefaultUsings();
        }

        private static string[] GetDefaultUsings() => [
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

        public bool SendErrors
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool EnableBraceCompletion
        {
            get;
            set => SetProperty(ref field, value);
        } = true;

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
            set => SetProperty(ref field, value);
        } = DefaultFontSize;

        public string EditorFontFamily
        {
            get;
            set => SetProperty(ref field, value);
        } = GetDefaultPlatformFontFamily();

        public double OutputFontSize
        {
            get;
            set => SetProperty(ref field, value);
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
            set => SetProperty(ref field, value);
        } = LiveModeDelayMsDefault;

        public bool SearchWhileTyping
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string DefaultPlatformName
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public double? WindowFontSize
        {
            get;
            set => SetProperty(ref field, value);
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
        }

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
