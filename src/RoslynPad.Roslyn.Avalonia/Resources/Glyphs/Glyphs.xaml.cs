using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RoslynPad.Roslyn.Resources;

public class Glyphs : ResourceDictionary
{
    public Glyphs()
    {
        AvaloniaXamlLoader.Load(this);
    }
}