using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Templates;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// Settings view for Avalonia.
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
}

public class SettingEditorTemplate : IDataTemplate
{
    public bool Match(object? data) => data is SettingItem;

    public Control? Build(object? data) => data is SettingItem setting
        ? CreateEditor(setting)
        : null;

    private static Control CreateEditor(SettingItem setting) => setting switch
    {
        { IsBoolean: true } => Bind(new CheckBox { VerticalAlignment = VerticalAlignment.Center }, ToggleButton.IsCheckedProperty, CompiledBinding.Create<SettingItem, bool>(x => x.BoolValue)),
        { IsEnum: true } => Bind(new ComboBox { ItemsSource = setting.EnumValues, VerticalAlignment = VerticalAlignment.Center, MinWidth = 150 }, ComboBox.SelectedItemProperty, CompiledBinding.Create<SettingItem, object?>(x => x.Value)),
        { IsNumeric: true } => Bind(new TextBox { VerticalAlignment = VerticalAlignment.Center, MinWidth = 100 }, TextBox.TextProperty, CompiledBinding.Create<SettingItem, double>(x => x.NumericValue), UpdateSourceTrigger.PropertyChanged),
        { IsStringArray: true } => Bind(new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinWidth = 200, MinHeight = 60 }, TextBox.TextProperty, CompiledBinding.Create<SettingItem, string?>(x => x.StringArrayValue), UpdateSourceTrigger.LostFocus),
        { IsString: true } => Bind(new TextBox { VerticalAlignment = VerticalAlignment.Center, MinWidth = 200 }, TextBox.TextProperty, CompiledBinding.Create<SettingItem, string?>(x => x.StringValue), UpdateSourceTrigger.PropertyChanged),
        _ => new TextBlock()
    };

    private static TControl Bind<TControl, TValue>(TControl control, Avalonia.AvaloniaProperty<TValue> property, CompiledBinding binding, UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default)
        where TControl : Control
    {
        binding.Mode = BindingMode.TwoWay;
        binding.UpdateSourceTrigger = updateSourceTrigger;
        control.Bind(property, binding);

        return control;
    }
}
