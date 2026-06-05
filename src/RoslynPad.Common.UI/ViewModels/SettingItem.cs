using System.Reflection;
using NuGet.Versioning;

namespace RoslynPad.UI;

/// <summary>
/// Represents a single editable setting.
/// </summary>
public class SettingItem(PropertyInfo property, IApplicationSettingsValues settings, string displayName, string? description, Type propertyType) : NotificationObject
{
    private const string ValidationErrorId = "Validation";
    private object? _value = property.GetValue(settings);

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
        get => _value;
        set => SetValue(value, nameof(Value));
    }

    public bool BoolValue
    {
        get => Value is true;
        set => Value = value;
    }

    public string? StringValue
    {
        get => Value as string;
        set => SetValue(value, nameof(StringValue));
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
                SetValue((int)value, nameof(NumericValue));
            }
            else if (PropertyType == typeof(double) || PropertyType == typeof(double?))
            {
                SetValue(value, nameof(NumericValue));
            }
        }
    }

    public string? StringArrayValue
    {
        get => Value is string[] arr ? string.Join(Environment.NewLine, arr) : null;
        set => SetValue(value?.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [], nameof(StringArrayValue));
    }

    private bool IsRequiredString => Property.Name is nameof(IApplicationSettingsValues.EditorFontFamily);

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

    private void SetValue(object? value, string sourcePropertyName)
    {
        ClearErrors(nameof(StringValue));
        ClearErrors(nameof(NumericValue));
        ClearErrors(nameof(StringArrayValue));

        value = NormalizeValue(value);
        var validationError = ValidateValue(value);
        if (validationError is not null)
        {
            SetError(sourcePropertyName, ValidationErrorId, validationError);
            return;
        }

        if (SetProperty(ref _value, value, nameof(Value)))
        {
            Property.SetValue(Settings, value);
            _value = Property.GetValue(Settings);
            OnPropertyChanged(nameof(BoolValue));
            OnPropertyChanged(nameof(StringValue));
            OnPropertyChanged(nameof(NumericValue));
            OnPropertyChanged(nameof(StringArrayValue));
        }
    }

    private object? NormalizeValue(object? value)
    {
        if (PropertyType == typeof(string))
        {
            var stringValue = value as string;
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return IsRequiredString ? string.Empty : null;
            }

            stringValue = stringValue.Trim();
            if (Property.Name == nameof(IApplicationSettingsValues.DefaultPlatformName))
            {
                var lastSpaceIndex = stringValue.LastIndexOf(' ');
                if (lastSpaceIndex >= 0 && NuGetVersion.TryParse(stringValue[(lastSpaceIndex + 1)..], out var version))
                {
                    return version.ToNormalizedString();
                }
            }

            return stringValue;
        }

        return value;
    }

    private string? ValidateValue(object? value)
    {
        if (Property.Name == nameof(IApplicationSettingsValues.EditorFontFamily) && value is not string { Length: > 0 })
        {
            return "Enter at least one font family.";
        }

        switch (Property.Name)
        {
            case nameof(IApplicationSettingsValues.DocumentPath) when value is string documentPath && !Directory.Exists(documentPath):
                return "Enter an existing directory, or leave this empty.";

            case nameof(IApplicationSettingsValues.SdkLocation) when value is string sdkLocation && !Directory.Exists(sdkLocation):
                return "Enter an existing .NET SDK root directory, or leave this empty.";

            case nameof(IApplicationSettingsValues.CustomThemePath) when value is string themePath && !File.Exists(themePath):
                return "Enter an existing VS Code theme file, or leave this empty.";

            case nameof(IApplicationSettingsValues.DefaultPlatformName) when value is string platformName && !NuGetVersion.TryParse(platformName, out _):
                return "Enter a .NET SDK version such as 10.0.300, or leave this empty.";

            case nameof(IApplicationSettingsValues.EditorFontSize) or nameof(IApplicationSettingsValues.OutputFontSize) when value is double fontSize && (fontSize < 8 || fontSize > 72):
                return "Enter a font size from 8 to 72.";

            case nameof(IApplicationSettingsValues.WindowFontSize) when value is double windowFontSize && windowFontSize <= 0:
                return "Enter a positive font size, or leave this empty.";

            case nameof(IApplicationSettingsValues.LiveModeDelayMs) when value is int liveModeDelayMs && liveModeDelayMs < 0:
                return "Enter a non-negative delay.";

            default:
                return null;
        }
    }
}
