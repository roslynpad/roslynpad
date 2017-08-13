using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Markup;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    public sealed class TaggedTextToTextBlockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as IEnumerable<TaggedText>)?.ToTextBlock();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}