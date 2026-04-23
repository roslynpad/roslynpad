namespace RoslynPad.Runtime;

/// <summary>
/// Secret storage manager.
/// </summary>
public interface ISecretManager
{
    /// <summary>
    /// Removes all stored secrets.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the value associated with the specified name, or null if not found.
    /// </summary>
    BinaryData? Get(string name);

    /// <summary>
    /// Gets all secrets as a single read operation.
    /// </summary>
    IReadOnlyDictionary<string, BinaryData> GetAll();

    /// <summary>
    /// Removes the secret with the specified name.
    /// </summary>
    /// <returns>true if the secret was found and removed; otherwise, false.</returns>
    bool Remove(string name);

    /// <summary>
    /// Sets the value for the specified name.
    /// </summary>
    void Set(string name, BinaryData value);
}
