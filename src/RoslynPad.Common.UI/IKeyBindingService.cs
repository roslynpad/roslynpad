namespace RoslynPad.UI;

/// <summary>
/// Manages key bindings for commands, supporting user customization and platform-specific defaults.
/// </summary>
public interface IKeyBindingService
{
    /// <summary>
    /// Gets the current key binding for a command.
    /// </summary>
    string GetKeyBinding(string command);

    /// <summary>
    /// Gets the description for a command, optionally including the current key binding.
    /// </summary>
    string GetDescription(string command, bool includeKeyBinding = false);

    /// <summary>
    /// Loads key binding overrides from settings.
    /// </summary>
    void LoadOverrides(IApplicationSettingsValues settings);
}
