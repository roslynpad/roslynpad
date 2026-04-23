namespace RoslynPad.Runtime;

/// <summary>
/// Extension methods for <see cref="SecretManager"/>.
/// </summary>
public static class SecretManagerExtensions
{
    /// <summary>
    /// Gets the string value for the specified name, or null if not found.
    /// </summary>
    public static string? GetString(this ISecretManager manager, string name) =>
        manager.Get(name)?.ToString();

    /// <summary>
    /// Sets a string value for the specified name.
    /// </summary>
    public static void SetString(this ISecretManager manager, string name, string value) =>
        manager.Set(name, BinaryData.FromString(value));

    /// <summary>
    /// Prompts the user to enter a secret value via the <see cref="Console"/> and stores it.
    /// </summary>
    public static void SetFromConsole(this ISecretManager manager, string name)
    {
        if (Console.ReadLine() is { Length: > 0 } value)
        {
            manager.SetString(name, value);
        }
    }
}
