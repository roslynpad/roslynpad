using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Morgania.CodeAnalysis.Editor.Resources;

public class Glyphs : ResourceDictionary
{
    public Glyphs()
    {
        AvaloniaXamlLoader.Load(this);
    }
}