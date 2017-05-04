namespace RoslynPad.Formatting
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;
    using Runtime;

    [ValueConversion(typeof(object), typeof(string))]
    public class ResultsFlatteningConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            var results = value as ObservableCollection<ResultObject>;
            if (results == null)
            {
                var type = value.GetType().ToString();
                var msg = $"{GetType().FullName} requires something castable to ObservableCollection<ResultObject> as a source. '{type}' isnt.";
                Debug.WriteLine(msg);
                return null;
                //throw new ArgumentException(msg,nameof(value));
            }

            var returnValue = string.Join(Environment.NewLine, results.Select(r => r.Value));
            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} - {GetType().FullName} was given {results.Count} items");
            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} - {GetType().FullName} is returning {returnValue}");
            return returnValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("This should not be convertable");
        }
    }
}