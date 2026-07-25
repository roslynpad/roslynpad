using Avalonia.Input;
using RoslynPad.UI;
using AvaloniaKeyBinding = Avalonia.Input.KeyBinding;

namespace RoslynPad;

/// <summary>
/// Helper for setting up key bindings from the KeyBindings service.
/// </summary>
internal static class KeyBindingHelper
{
    /// <summary>
    /// Creates a KeyBinding for the specified command, using the current key binding from settings.
    /// </summary>
    public static AvaloniaKeyBinding? CreateKeyBinding(string command, System.Windows.Input.ICommand boundCommand, object? commandParameter = null)
    {
        return GetKeyGesture(command) is { } gesture
            ? new AvaloniaKeyBinding
            {
                Gesture = gesture,
                Command = boundCommand,
                CommandParameter = commandParameter!
            }
            : null;
    }

    /// <summary>
    /// Gets the current (default or user-customized) gesture of a command from the
    /// KeyBindings service, or null when unbound or unparsable.
    /// </summary>
    public static KeyGesture? GetKeyGesture(string command)
    {
        var keySequence = KeyBindings.Service.GetKeyBinding(command);
        if (string.IsNullOrWhiteSpace(keySequence))
        {
            return null;
        }

        try
        {
            return KeyGesture.Parse(keySequence);
        }
        catch (FormatException)
        {
            System.Diagnostics.Debug.WriteLine($"KeyBindingHelper: Failed to parse gesture '{keySequence}' for command '{command}'");
            return null;
        }
    }

    /// <summary>
    /// Adds key bindings to a control's KeyBindings collection.
    /// </summary>
    public static void AddKeyBinding(this InputElement control, string command, System.Windows.Input.ICommand boundCommand, object? commandParameter = null)
    {
        var keyBinding = CreateKeyBinding(command, boundCommand, commandParameter);
        if (keyBinding is not null)
        {
            control.KeyBindings.Add(keyBinding);
        }
    }
}
