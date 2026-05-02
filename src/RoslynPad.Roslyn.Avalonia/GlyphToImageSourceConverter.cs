using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Roslyn;

public class GlyphToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value as Glyph?)?.ToImageSource() ?? BindingOperations.DoNothing;
    }

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
