using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace RoslynPad.Converters;

public sealed class LevelToIndentConverter : IValueConverter
{
    private const double IndentSize = 19.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new Thickness((int)(value ?? 0) * IndentSize, 0, 0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
