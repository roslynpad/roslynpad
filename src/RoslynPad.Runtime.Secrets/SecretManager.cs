using System.Text.Json;
using Microsoft.Identity.Client.Extensions.Msal;

namespace RoslynPad.Runtime;

/// <summary>
/// Cross-platform secret storage using OS-level encryption.
/// </summary>
public sealed class SecretManager : ISecretManager, IDisposable
{
    private readonly Storage _storage;
    private readonly string _lockFilePath;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Application name used to derive file names and platform-specific identifiers.
    /// </summary>
    public string AppName { get; }

    /// <summary>
    /// Directory where the secrets file is stored.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// Gets the default <see cref="SecretManager"/> instance using app name "roslynpad".
    /// </summary>
    public static SecretManager Default => field ??= new SecretManager("roslynpad");

    /// <summary>
    /// Creates a new <see cref="SecretManager"/> with the specified app name.
    /// </summary>
    /// <param name="appName">Application name used to derive file names and platform-specific identifiers.</param>
    /// <param name="directory">Optional directory for the secrets file. Defaults to ~/.{appName}/</param>
    public SecretManager(string appName, string? directory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);

        AppName = appName;
        Directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{appName}");
        System.IO.Directory.CreateDirectory(Directory);

        var fileName = $"{appName}.secrets";
        _lockFilePath = Path.Combine(Directory, $"{fileName}.lock");

        var storageProperties = BuildStorageProperties(appName, fileName, Directory);
        _storage = Storage.Create(storageProperties);

        _semaphore = new(1, 1);
    }

    /// <inheritdoc />
    public BinaryData? Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var secrets = ReadWithLock();
        return secrets.TryGetValue(name, out var value) ? value : null;
    }

    /// <inheritdoc />
    public void Set(string name, BinaryData value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        MutateWithLock(secrets => secrets[name] = value);
    }

    /// <inheritdoc />
    public bool Remove(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var removed = false;
        MutateWithLock(secrets => removed = secrets.Remove(name));
        return removed;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, BinaryData> GetAll() => ReadWithLock();

    /// <inheritdoc />
    public void Clear()
    {
        using (AcquireLock())
        {
            _storage.Clear();
        }
    }

    private Dictionary<string, BinaryData> ReadWithLock()
    {
        using (AcquireLock())
        {
            return ReadSecrets();
        }
    }

    private void MutateWithLock(Action<Dictionary<string, BinaryData>> mutation)
    {
        using (AcquireLock())
        {
            var secrets = ReadSecrets();
            mutation(secrets);

            var data = JsonSerializer.SerializeToUtf8Bytes(secrets);
            _storage.WriteData(data);
        }
    }

    private Dictionary<string, BinaryData> ReadSecrets()
    {
        var data = _storage.ReadData();
        if (data.Length == 0)
        {
            return new Dictionary<string, BinaryData>(StringComparer.Ordinal);
        }

        return JsonSerializer.Deserialize<Dictionary<string, BinaryData>>(data)
            ?? new Dictionary<string, BinaryData>(StringComparer.Ordinal);
    }

    private CrossPlatLockWithSemaphore AcquireLock()
    {
        _semaphore.Wait();
        try
        {
            return new CrossPlatLockWithSemaphore(_lockFilePath, _semaphore);
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }

    private static StorageCreationProperties BuildStorageProperties(string appName, string fileName, string directory)
    {
        var builder = new StorageCreationPropertiesBuilder(fileName, directory)
            .WithMacKeyChain(appName, appName)
            .WithLinuxKeyring(
                schemaName: appName,
                collection: "default",
                secretLabel: $"{appName}-secrets",
                attribute1: new("application", appName),
                attribute2: new("purpose", "secrets"));

        return builder.Build();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

#pragma warning disable CA1001
    private readonly struct CrossPlatLockWithSemaphore(string lockFilePath, SemaphoreSlim semaphore) : IDisposable
#pragma warning restore CA1001
    {
        private readonly CrossPlatLock _lock = new(lockFilePath);

        public void Dispose()
        {
            _lock.Dispose();
            semaphore.Release();
        }
    }
}
