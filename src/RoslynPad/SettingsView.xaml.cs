using System.Windows;
using System.Windows.Controls;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
}

/// <summary>
/// Template selector for different setting types.
/// </summary>
public class SettingTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BooleanTemplate { get; set; }
    public DataTemplate? StringTemplate { get; set; }
    public DataTemplate? NumericTemplate { get; set; }
    public DataTemplate? EnumTemplate { get; set; }
    public DataTemplate? StringArrayTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is not SettingItem setting)
            return base.SelectTemplate(item, container);

        return setting switch
        {
            { IsBoolean: true } => BooleanTemplate,
            { IsEnum: true } => EnumTemplate,
            { IsNumeric: true } => NumericTemplate,
            { IsStringArray: true } => StringArrayTemplate,
            { IsString: true } => StringTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}
