using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Microsoft.CodeAnalysis.CodeActions;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.CodeActions;

namespace RoslynPad.Formatting;

internal sealed class CodeActionToGlyphConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value as CodeAction)?.GetGlyph().ToImageSource();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}