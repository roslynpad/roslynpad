using System.Reflection;

namespace RoslynPad.UI;

/// <summary>
/// Represents a single editable setting.
/// </summary>
public class SettingItem(PropertyInfo property, IApplicationSettingsValues settings, string displayName, string? description, Type propertyType) : NotificationObject
{
    public PropertyInfo Property { get; } = property;
    public IApplicationSettingsValues Settings { get; } = settings;
    public string DisplayName { get; } = displayName;
    public string? Description { get; } = description;
    public Type PropertyType { get; } = propertyType;

    internal Type NonNullablePropertyType => Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;

    public bool IsBoolean => PropertyType == typeof(bool);
    public bool IsNumeric => PropertyType == typeof(int) || PropertyType == typeof(double) || PropertyType == typeof(double?);
    public bool IsString => PropertyType == typeof(string);
    public bool IsEnum => NonNullablePropertyType.IsEnum;
    public bool IsStringArray => PropertyType == typeof(string[]);

    public object? Value
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Property.SetValue(Settings, value);
            }
        }
    } = property.GetValue(settings);

    public bool BoolValue
    {
        get => Value is true;
        set => Value = value;
    }

    public string? StringValue
    {
        get => Value as string;
        set => Value = value;
    }

    public double NumericValue
    {
        get => Value switch
        {
            int i => i,
            double d => d,
            _ => 0
        };
        set
        {
            if (PropertyType == typeof(int))
            {
                Value = (int)value;
            }
            else if (PropertyType == typeof(double) || PropertyType == typeof(double?))
            {
                Value = value;
            }
        }
    }

    public string? StringArrayValue
    {
        get => Value is string[] arr ? string.Join(Environment.NewLine, arr) : null;
        set => Value = value?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }

    public System.Collections.IEnumerable? EnumValues => IsEnum
        ? PropertyType == NonNullablePropertyType
            ? Enum.GetValues(NonNullablePropertyType)
            : Enum.GetValues(NonNullablePropertyType).Cast<object?>().Prepend(null)
        : null;

    public bool MatchesFilter(string[]? filterTerms)
    {
        if (filterTerms is null || filterTerms.Length == 0)
        {
            return true;
        }

        // All terms must match (AND logic)
        foreach (var term in filterTerms)
        {
            if (!DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                !(Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return false;
            }
        }

        return true;
    }
}
