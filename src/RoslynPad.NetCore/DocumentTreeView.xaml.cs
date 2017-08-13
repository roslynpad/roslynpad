using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RoslynPad
{
    class DocumentTreeView : UserControl
    {
        public DocumentTreeView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
