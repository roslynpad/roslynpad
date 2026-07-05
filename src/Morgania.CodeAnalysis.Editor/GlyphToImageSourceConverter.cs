using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Converts an image-catalog moniker name (e.g. a view model's <c>GlyphName</c>)
/// to its themed <see cref="ImageCatalog"/> image.
/// </summary>
internal class GlyphToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is string name ? ImageCatalog.GetImage(name) : null) ?? BindingOperations.DoNothing;
    }

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
