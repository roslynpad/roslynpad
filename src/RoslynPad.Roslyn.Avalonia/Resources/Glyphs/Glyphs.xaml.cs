using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RoslynPad.Roslyn.Resources
{
    internal class Glyphs : ResourceDictionary
    {
        public Glyphs()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}