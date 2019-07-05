using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia")]

namespace Avalonia
{
    public static class Templates
    {
        public static readonly AvaloniaProperty<DataTemplate> HeaderTemplateProperty =
            AvaloniaProperty.RegisterAttached<Control, DataTemplate>("HeaderTemplate", typeof(Templates));

        public static DataTemplate GetHeaderTemplate(Control control) => control.GetValue(HeaderTemplateProperty);
        public static void SetHeaderTemplate(Control control, DataTemplate value) => control.SetValue(HeaderTemplateProperty, value);
    }
}
