using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Microsoft.CodeAnalysis;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Maps a dock document's view model to its tab icon resource.
/// </summary>
public class DocumentIconConverter : IValueConverter
{
    public static readonly DocumentIconConverter Instance = new();

    public static readonly IValueConverter HasIcon = new FuncValueConverter<object?, bool>(value => GetIconKey(value) is not null);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        GetIconKey(value) is { } key && Application.Current?.TryGetResource(key, null, out var icon) == true ? icon : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();

    private static string? GetIconKey(object? value) => value switch
    {
        SettingsViewModel => "Settings",
        SecretsViewModel => "Secrets",
        OpenDocumentViewModel { SourceCodeKind: SourceCodeKind.Script } => "Script",
        OpenDocumentViewModel => "CsFile",
        MetadataDocumentViewModel => "CsFile",
        _ => null,
    };
}
