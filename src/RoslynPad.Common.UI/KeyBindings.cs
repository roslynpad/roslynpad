namespace RoslynPad.UI;

/// <summary>
/// Static accessor for key binding service (for use in places where DI is not available like XAML converters).
/// </summary>
public static class KeyBindings
{
    public static IKeyBindingService Service
    {
        get => field ?? throw new InvalidOperationException("KeyBindingService has not been initialized.");
        set;
    }
}
