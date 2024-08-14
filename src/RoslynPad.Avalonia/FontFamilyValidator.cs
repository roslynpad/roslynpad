using RoslynPad.UI.Services;
using Avalonia.Media;
using RoslynPad.UI;
using System.Composition;

namespace RoslynPad;

[Export(typeof(IFontFamilyValidator)), Shared]
internal class FontFamilyValidator : IFontFamilyValidator
{
    public bool IsValid(string fontFamilyName)
    {
        try
        {
            var fontFamily = FontFamily.Parse(fontFamilyName);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
