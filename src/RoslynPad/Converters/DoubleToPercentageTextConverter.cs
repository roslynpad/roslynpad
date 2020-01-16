using System;
using System.Globalization;
using System.Windows.Data;

namespace RoslynPad.Converters
{
    public class DoubleToPercentageTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent;

            if (value is double?)
            {
                percent = (value as double?) ?? 0;
            }
            else
            {
                try
                {
                    percent = (double)value;
                }
                catch
                {
                    percent = 0.0;
                }
            }

            if (percent <= 0) return "0%";
            if (percent >= 1) return "100%";

            return ((int)Math.Round(percent * 100.0, 0)) + "%";
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
