using System.Composition;

namespace RoslynPad.UI;

[Export(typeof(IKeyBindingService)), Shared]
internal sealed class KeyBindingService : IKeyBindingService
{
    private readonly Dictionary<string, string> _currentBindings = new();
    private readonly object _lock = new();

    public KeyBindingService()
    {
        ResetToDefaults();
    }

    public string GetKeyBinding(string command)
    {
        lock (_lock)
        {
            return _currentBindings.TryGetValue(command, out var binding) ? binding : string.Empty;
        }
    }

    public string GetDescription(string command, bool includeKeyBinding = false)
    {
        if (!KeyBindingCommands.All.TryGetValue(command, out var info))
        {
            return string.Empty;
        }

        if (!includeKeyBinding)
        {
            return info.Description;
        }

        var keyBinding = GetKeyBinding(command);
        return string.IsNullOrEmpty(keyBinding)
            ? info.Description
            : $"{info.Description} ({keyBinding})";
    }

    public void LoadOverrides(IApplicationSettingsValues settings)
    {
        lock (_lock)
        {
            ResetToDefaults();

            var keyBindings = settings.KeyBindings;
            if (keyBindings == null)
            {
                return;
            }

            foreach (var binding in keyBindings)
            {
                if (string.IsNullOrWhiteSpace(binding.Command) || string.IsNullOrWhiteSpace(binding.Key))
                {
                    continue;
                }

                if (KeyBindingCommands.All.ContainsKey(binding.Command))
                {
                    _currentBindings[binding.Command] = binding.Key;
                }
            }
        }
    }

    private void ResetToDefaults()
    {
        _currentBindings.Clear();
        foreach (var (command, info) in KeyBindingCommands.All)
        {
            _currentBindings[command] = info.GetDefaultKey();
        }
    }
}
