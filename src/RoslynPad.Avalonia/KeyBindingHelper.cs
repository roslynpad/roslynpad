using Avalonia.Controls;
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
        var keySequence = KeyBindings.Service.GetKeyBinding(command);
        if (string.IsNullOrWhiteSpace(keySequence))
        {
            return null;
        }

        try
        {
            var gesture = KeyGesture.Parse(keySequence);
            return new AvaloniaKeyBinding
            {
                Gesture = gesture,
                Command = boundCommand,
                CommandParameter = commandParameter!
            };
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
