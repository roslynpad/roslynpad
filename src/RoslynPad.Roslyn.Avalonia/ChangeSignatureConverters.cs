using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RoslynPad.Roslyn;

public static class ChangeSignatureConverters
{
    public static readonly IValueConverter RemovedToOpacity = new RemovedToOpacityConverter();
    public static readonly IValueConverter RemovedToForeground = new RemovedToForegroundConverter();

    private class RemovedToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? 0.5 : 1.0;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private class RemovedToForegroundConverter : IValueConverter
    {
        private static readonly IBrush s_normalBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E));
        private static readonly IBrush s_removedBrush = Brushes.Gray;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? s_removedBrush : s_normalBrush;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
