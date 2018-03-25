using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RoslynPad
{
    class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
