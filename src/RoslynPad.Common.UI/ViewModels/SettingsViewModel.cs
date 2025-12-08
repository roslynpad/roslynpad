using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RoslynPad.UI;

/// <summary>
/// ViewModel for the Settings Editor.
/// </summary>
public partial class SettingsViewModel : NotificationObject, IDocumentContent
{
    private readonly IApplicationSettingsValues _settings;

    public SettingsViewModel(IApplicationSettingsValues settings)
    {
        _settings = settings;
        AllSettings = CreateSettingItems();
        FilteredSettings = new ObservableCollection<SettingItem>(AllSettings);
    }

    public string Title => "Settings";

    public bool IsDirty => false;

    public ObservableCollection<SettingItem> AllSettings { get; }
    public ObservableCollection<SettingItem> FilteredSettings { get; }

    public string? FilterText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    }

    private void ApplyFilter()
    {
        var filterTerms = ParseFilterTerms(FilterText);

        FilteredSettings.Clear();
        foreach (var setting in AllSettings)
        {
            if (setting.MatchesFilter(filterTerms))
            {
                FilteredSettings.Add(setting);
            }
        }
    }

    private static string[]? ParseFilterTerms(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return null;

        // Split by whitespace and punctuation marks
        var terms = NonWhitespaceRegex().Split(filter).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
        return terms.Length > 0 ? terms : null;
    }

    private ObservableCollection<SettingItem> CreateSettingItems()
    {
        var items = new ObservableCollection<SettingItem>();
        var properties = typeof(IApplicationSettingsValues).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Skip read-only or internal properties
            if (!property.CanWrite)
            {
                continue;
            }

            // Skip properties we don't want to expose in the UI
            var browsable = property.GetCustomAttribute<BrowsableAttribute>();
            if (browsable?.Browsable == false)
            {
                continue;
            }

            var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
            var displayName = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? FormatPropertyName(property.Name);

            items.Add(new SettingItem(property, _settings, displayName, description, property.PropertyType));
        }

        return items;
    }

    private static string FormatPropertyName(string name)
    {
        // Insert spaces before capital letters
        var result = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
            {
                result.Append(' ');
            }

            result.Append(c);
        }

        return result.ToString();
    }

    [GeneratedRegex(@"\W")]
    private static partial Regex NonWhitespaceRegex();
}
