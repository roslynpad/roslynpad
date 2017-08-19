using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Microsoft.CodeAnalysis.CodeActions;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.CodeActions;

namespace RoslynPad.Formatting
{
    internal sealed class CodeActionToGlyphConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((CodeAction)value).GetGlyph().ToImageSource();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}