using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using RoslynPad.UI;

namespace RoslynPad.Converters;

public class ControlCharacterInlinesConverter : IValueConverter
{
    public static ControlCharacterInlinesConverter Instance { get; } = new();

    public Brush BackgroundBrush { get; set; } = Brushes.LightGray;

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        return Get((string)value);

        IEnumerable<Inline> Get(string s)
        {
            var lastIndex = 0;
            foreach (var index in StringSearch.GetIndices(s, CharSearchValues.ControlChars))
            {
                if (lastIndex != index)
                {
                    yield return new Run(s[lastIndex..index]);
                    yield return new Run(" ") { FontSize = 5 };
                }

                yield return new Run(((int)s[index]).ToString("x2", CultureInfo.InvariantCulture)) { Background = BackgroundBrush };
                yield return new Run(" ") { FontSize = 5 };
                lastIndex = index + 1;
            }

            if (lastIndex < s.Length)
            {
                yield return new Run(s[lastIndex..]);
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
