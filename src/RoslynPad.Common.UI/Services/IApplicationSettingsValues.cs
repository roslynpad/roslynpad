﻿using System.ComponentModel;
using RoslynPad.Themes;

namespace RoslynPad.UI;

public interface IApplicationSettingsValues : INotifyPropertyChanged
{
    bool SendErrors { get; set; }
    bool EnableBraceCompletion { get; set; }
    string? LatestVersion { get; set; }
    string? WindowBounds { get; set; }
    string? DockLayout { get; set; }
    string? WindowState { get; set; }
    double EditorFontSize { get; set; }
    string EditorFontFamily { get; set; }
    double OutputFontSize { get; set; }
    string? DocumentPath { get; set; }
    bool SearchFileContents { get; set; }
    bool SearchUsingRegex { get; set; }
    bool OptimizeCompilation { get; set; }
    int LiveModeDelayMs { get; set; }
    bool SearchWhileTyping { get; set; }
    string DefaultPlatformName { get; set; }
    double? WindowFontSize { get; set; }
    bool FormatDocumentOnComment { get; set; }
    string EffectiveDocumentPath { get; }
    string? CustomThemePath { get; }
    ThemeType? CustomThemeType { get; }
    BuiltInTheme BuiltInTheme { get; }
}
