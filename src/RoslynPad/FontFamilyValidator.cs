using System.Composition;
using System.Drawing;
using RoslynPad.UI;
using RoslynPad.UI.Services;

namespace RoslynPad;

[Export(typeof(IFontFamilyValidator)), Shared]
internal class FontFamilyValidator : IFontFamilyValidator
{
    public bool IsValid(string fontFamilyName)
    {
        var fontFamily = new FontFamily(fontFamilyName);
        
        return fontFamily.IsStyleAvailable(FontStyle.Regular);
    }
}
