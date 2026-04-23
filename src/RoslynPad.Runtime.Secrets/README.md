# RoslynPad.Runtime.Secrets

Cross-platform secret storage using OS-level encryption (DPAPI on Windows, Keychain on macOS, Keyring on Linux).

## Key Types

### `SecretManager`

Thread-safe secret storage backed by encrypted files.

| Member | Description |
|--------|-------------|
| `Default` | Singleton instance (app name `"roslynpad"`) that works with the secrets view in RoslynPad |
| `SecretManager(string appName, string? directory)` | Create a custom instance; stores secrets in `~/.{appName}/` |

### `ISecretManager`

Interface for secret operations.

| Method | Description |
|--------|-------------|
| `Get(string name)` | Get a secret as `BinaryData`, or `null` |
| `GetAll()` | Get all secrets in a single read |
| `Set(string name, BinaryData value)` | Store a secret |
| `Remove(string name)` | Remove a secret, returns `true` if found |
| `Clear()` | Remove all secrets |

### Extension Methods

Convenience methods on `SecretManager` for string values.

| Method | Description |
|--------|-------------|
| `GetString(string name)` | Get a secret as `string?` |
| `SetString(string name, string value)` | Store a string secret |
| `SetFromConsole(string name)` | Prompt the user to enter a secret via `Console` |

## Usage

```csharp
// Store and retrieve strings
SecretManager.Default.SetString("ApiKey", "sk-...");
string? key = SecretManager.Default.GetString("ApiKey");

// Remove a secret
SecretManager.Default.Remove("ApiKey");

// Prompt the user to enter a secret via Console
SecretManager.Default.SetFromConsole("ApiKey");
```
