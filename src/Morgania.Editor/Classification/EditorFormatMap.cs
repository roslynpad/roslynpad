#nullable enable

namespace Microsoft.VisualStudio.Text.Classification.Implementation;

using Avalonia.Controls;

/// <summary>
/// Editor format map over the composed <see cref="EditorFormatDefinition"/> exports.
/// Keys are definition names; values are the resource dictionaries the definitions create,
/// overlaid with any properties set at run time.
/// </summary>
internal sealed class EditorFormatMap : IEditorFormatMap
{
    private readonly Dictionary<string, Lazy<EditorFormatDefinition, EditorFormatMetadata>> _definitions;
    private readonly Dictionary<string, ResourceDictionary> _properties = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _pendingChanges = [];
    private bool _isInBatchUpdate;

    public EditorFormatMap(IEnumerable<Lazy<EditorFormatDefinition, EditorFormatMetadata>> definitions)
    {
        _definitions = new Dictionary<string, Lazy<EditorFormatDefinition, EditorFormatMetadata>>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            if (definition.Metadata.Name is { } name)
            {
                // Later exports do not replace earlier ones, matching v1 first-wins behavior.
                _definitions.TryAdd(name, definition);
            }
        }
    }

    public event EventHandler<FormatItemsEventArgs>? FormatMappingChanged;

    public bool IsInBatchUpdate => _isInBatchUpdate;

    public ResourceDictionary GetProperties(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_properties.TryGetValue(key, out var properties))
        {
            return properties;
        }

        properties = _definitions.TryGetValue(key, out var definition)
            ? definition.Value.CreateResourceDictionary()
            : [];
        _properties[key] = properties;
        return properties;
    }

    public void AddProperties(string key, ResourceDictionary properties)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(properties);
        _properties[key] = properties;
        RaiseChanged(key);
    }

    public void SetProperties(string key, ResourceDictionary properties)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(properties);
        _properties[key] = properties;
        RaiseChanged(key);
    }

    public void ClearProperties()
    {
        var keys = _properties.Keys.ToList();
        _properties.Clear();
        foreach (var key in keys)
        {
            RaiseChanged(key);
        }
    }

    public void ClearProperties(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_properties.Remove(key))
        {
            RaiseChanged(key);
        }
    }

    public void BeginBatchUpdate()
    {
        if (_isInBatchUpdate)
        {
            throw new InvalidOperationException("The map is already in a batch update.");
        }

        _isInBatchUpdate = true;
    }

    public void EndBatchUpdate()
    {
        if (!_isInBatchUpdate)
        {
            throw new InvalidOperationException("The map is not in a batch update.");
        }

        _isInBatchUpdate = false;
        if (_pendingChanges.Count > 0)
        {
            var changes = new List<string>(_pendingChanges).AsReadOnly();
            _pendingChanges.Clear();
            FormatMappingChanged?.Invoke(this, new FormatItemsEventArgs(changes));
        }
    }

    private void RaiseChanged(string key)
    {
        if (_isInBatchUpdate)
        {
            _pendingChanges.Add(key);
        }
        else
        {
            FormatMappingChanged?.Invoke(this, new FormatItemsEventArgs(new List<string> { key }.AsReadOnly()));
        }
    }
}
