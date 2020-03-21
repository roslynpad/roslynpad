using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace RoslynPad.Converters
{
    public class DoubleToPercentageTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var percent = value as double? ?? 0;
            if (percent <= 0) percent = 0;
            if (percent >= 1) percent = 1;

            return ((int)Math.Round(percent * 100.0, 0)) + "%";
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
