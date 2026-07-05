using System.Globalization;
using Avalonia.Data.Converters;

namespace Morgania.CodeAnalysis.Editor;

public static class ChangeSignatureConverters
{
    public static readonly IValueConverter RemovedToOpacity = new RemovedToOpacityConverter();

    private class RemovedToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? 0.5 : 1.0;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
