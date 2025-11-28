namespace RoslynPad.Converters;

public class CommandToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string command)
            return UI.KeyBindings.Service.GetDescription(command, includeKeyBinding: true);
        return DependencyProperty.UnsetValue;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// Converts a key binding string (e.g., "Ctrl+S") to a KeyGesture, Key, or ModifierKeys.
/// Can take the key binding string from either the value or the ConverterParameter (command string).
/// When ConverterParameter is a command string, looks up the key binding from KeyBindings.Service.
/// </summary>
public class KeySequenceToGestureConverter : IValueConverter
{
    private static readonly KeyGestureConverter s_gestureConverter = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // If parameter is a command string, look up the key binding
        var keySequence = parameter is string command && UI.KeyBindingCommands.All.ContainsKey(command)
            ? UI.KeyBindings.Service.GetKeyBinding(command)
            : value as string;

        if (string.IsNullOrWhiteSpace(keySequence))
            return DependencyProperty.UnsetValue;

        try
        {
            var gesture = (KeyGesture?)s_gestureConverter.ConvertFromString(keySequence);
            if (gesture == null)
                return DependencyProperty.UnsetValue;

            if (targetType == typeof(KeyGesture))
                return gesture;
            if (targetType == typeof(ModifierKeys))
                return gesture.Modifiers;
            if (targetType == typeof(Key))
                return gesture.Key;
        }
        catch (FormatException ex)
        {
            System.Diagnostics.Debug.WriteLine($"KeySequenceToGestureConverter: FormatException parsing '{keySequence}': {ex}");
        }
        catch (NotSupportedException ex)
        {
            System.Diagnostics.Debug.WriteLine($"KeySequenceToGestureConverter: NotSupportedException parsing '{keySequence}': {ex}");
        }
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
