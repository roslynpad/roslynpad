using System.ComponentModel;
using RoslynPad.Themes;

namespace RoslynPad.UI;

public interface IApplicationSettingsValues : INotifyPropertyChanged
{
    [Browsable(false)]
    IList<KeyBinding>? KeyBindings { get; set; }

    [Description("Send error reports to help improve RoslynPad. Only internal errors are sent, not your code.")]
    bool SendErrors { get; set; }

    [Description("Automatically insert closing braces, brackets, and parentheses.")]
    bool EnableBraceCompletion { get; set; }

    [Browsable(false)]
    string? LatestVersion { get; set; }

    [Browsable(false)]
    string? WindowBounds { get; set; }

    [Browsable(false)]
    string? DockLayout { get; set; }

    [Browsable(false)]
    string? WindowState { get; set; }

    [Description("Font size for the code editor (8-72).")]
    double EditorFontSize { get; set; }

    [Description("Font family for the code editor. Separate multiple fonts with commas for fallback.")]
    string EditorFontFamily { get; set; }

    [Description("Font size for the results output panel.")]
    double OutputFontSize { get; set; }

    [Description("Custom path for storing documents. Leave empty to use the default RoslynPad folder.")]
    string? DocumentPath { get; set; }

    [Description("Search within file contents when searching documents.")]
    bool SearchFileContents { get; set; }

    [Description("Use regular expressions when searching documents.")]
    bool SearchUsingRegex { get; set; }

    [Description("Enable Release mode compilation for better performance.")]
    bool OptimizeCompilation { get; set; }

    [Description("Delay in milliseconds before running code in Live Mode.")]
    int LiveModeDelayMs { get; set; }

    [Description("Search documents as you type without pressing Enter.")]
    bool SearchWhileTyping { get; set; }

    [Description("Default .NET platform for new documents (e.g., 'net8.0').")]
    string DefaultPlatformName { get; set; }

    [Description("Font size for the application window (optional).")]
    double? WindowFontSize { get; set; }

    [Description("Automatically format the document after uncommenting code.")]
    bool FormatDocumentOnComment { get; set; }

    [Browsable(false)]
    string EffectiveDocumentPath { get; }

    [Description("Path to a custom VS Code theme file (.json).")]
    string? CustomThemePath { get; set; }

    [Description("Theme type when using a custom theme (Light or Dark).")]
    ThemeType? CustomThemeType { get; set; }

    [Description("Built-in theme to use (System, Light, or Dark).")]
    BuiltInTheme BuiltInTheme { get; set; }

    [Description("Default using directives for new documents. One per line.")]
    string[]? DefaultUsings { get; set; }
}
