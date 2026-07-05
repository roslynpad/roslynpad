#nullable enable

namespace Microsoft.VisualStudio.Text.Classification.Implementation;

using System.Collections.ObjectModel;
using System.Globalization;

using Avalonia.Controls;
using Avalonia.Media;

using Microsoft.VisualStudio.Text.Formatting;

/// <summary>
/// Maps classification types to <see cref="TextFormattingRunProperties"/> by converting the
/// resource dictionaries of the corresponding editor-format definitions. Properties of a
/// classification type are merged over those of its base types (nearest definition wins per
/// property), and finally over <see cref="DefaultTextProperties"/> at render time.
/// </summary>
internal sealed class ClassificationFormatMap : IClassificationFormatMap
{
    private readonly IEditorFormatMap _editorFormatMap;
    private readonly Dictionary<string, string> _keyByClassification;
    private readonly List<IClassificationType> _priorityOrder;
    private readonly Dictionary<IClassificationType, TextFormattingRunProperties> _cache = [];
    private TextFormattingRunProperties _defaultTextProperties;
    private int _batchDepth;
    private bool _pendingChange;

    public ClassificationFormatMap(
        IEditorFormatMap editorFormatMap,
        IClassificationTypeRegistryService classificationTypeRegistry,
        IEnumerable<Lazy<EditorFormatDefinition, ClassificationFormatMetadata>> orderedDefinitions)
    {
        _editorFormatMap = editorFormatMap;
        _keyByClassification = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _priorityOrder = [];
        foreach (var definition in orderedDefinitions)
        {
            if (definition.Metadata.Name is not { } key)
            {
                continue;
            }

            foreach (var typeName in definition.Metadata.ClassificationTypeNames ?? [])
            {
                if (classificationTypeRegistry.GetClassificationType(typeName) is { } classificationType)
                {
                    if (_keyByClassification.TryAdd(typeName, key))
                    {
                        _priorityOrder.Add(classificationType);
                    }
                }
            }
        }

        _defaultTextProperties = TextFormattingRunProperties.CreateTextFormattingRunProperties();
        _editorFormatMap.FormatMappingChanged += (_, _) =>
        {
            _cache.Clear();
            RaiseChanged();
        };
    }

    public event EventHandler<EventArgs>? ClassificationFormatMappingChanged;

    public TextFormattingRunProperties DefaultTextProperties
    {
        get => _defaultTextProperties;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!_defaultTextProperties.Equals(value))
            {
                _defaultTextProperties = value;
                _cache.Clear();
                RaiseChanged();
            }
        }
    }

    public ReadOnlyCollection<IClassificationType> CurrentPriorityOrder => _priorityOrder.AsReadOnly();

    public bool IsInBatchUpdate => _batchDepth > 0;

    public string GetEditorFormatMapKey(IClassificationType classificationType)
    {
        ArgumentNullException.ThrowIfNull(classificationType);
        return _keyByClassification.TryGetValue(classificationType.Classification, out var key)
            ? key
            : classificationType.Classification;
    }

    public TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType)
    {
        ArgumentNullException.ThrowIfNull(classificationType);
        return FromResourceDictionary(_editorFormatMap.GetProperties(GetEditorFormatMapKey(classificationType)));
    }

    public TextFormattingRunProperties GetTextProperties(IClassificationType classificationType)
    {
        ArgumentNullException.ThrowIfNull(classificationType);
        if (_cache.TryGetValue(classificationType, out var cached))
        {
            return cached;
        }

        var properties = MergeLineage(classificationType, []);
        properties = Merge(properties, _defaultTextProperties);
        _cache[classificationType] = properties;
        return properties;
    }

    public void SetTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties)
        => SetExplicitTextProperties(classificationType, properties);

    public void SetExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties)
    {
        ArgumentNullException.ThrowIfNull(classificationType);
        ArgumentNullException.ThrowIfNull(properties);
        _editorFormatMap.SetProperties(GetEditorFormatMapKey(classificationType), ToResourceDictionary(properties));
    }

    public void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties)
    {
        ArgumentNullException.ThrowIfNull(classificationType);
        ArgumentNullException.ThrowIfNull(properties);
        if (!_keyByClassification.ContainsKey(classificationType.Classification))
        {
            _keyByClassification[classificationType.Classification] = classificationType.Classification;
            _priorityOrder.Insert(0, classificationType);
        }

        _editorFormatMap.AddProperties(GetEditorFormatMapKey(classificationType), ToResourceDictionary(properties));
    }

    public void AddExplicitTextProperties(IClassificationType classificationType, TextFormattingRunProperties properties, IClassificationType priority)
    {
        ArgumentNullException.ThrowIfNull(priority);
        AddExplicitTextProperties(classificationType, properties);
        int index = _priorityOrder.IndexOf(classificationType);
        int priorityIndex = _priorityOrder.IndexOf(priority);
        if (index >= 0 && priorityIndex >= 0)
        {
            _priorityOrder.RemoveAt(index);
            priorityIndex = _priorityOrder.IndexOf(priority);
            _priorityOrder.Insert(priorityIndex, classificationType);
        }
    }

    public void SwapPriorities(IClassificationType firstType, IClassificationType secondType)
    {
        ArgumentNullException.ThrowIfNull(firstType);
        ArgumentNullException.ThrowIfNull(secondType);
        int firstIndex = _priorityOrder.IndexOf(firstType);
        int secondIndex = _priorityOrder.IndexOf(secondType);
        if (firstIndex < 0 || secondIndex < 0)
        {
            throw new ArgumentException("Both classification types must be in the priority order.");
        }

        (_priorityOrder[firstIndex], _priorityOrder[secondIndex]) = (_priorityOrder[secondIndex], _priorityOrder[firstIndex]);
        _cache.Clear();
        RaiseChanged();
    }

    public void BeginBatchUpdate()
    {
        _batchDepth++;
        if (!_editorFormatMap.IsInBatchUpdate)
        {
            _editorFormatMap.BeginBatchUpdate();
        }
    }

    public void EndBatchUpdate()
    {
        if (_batchDepth == 0)
        {
            throw new InvalidOperationException("The map is not in a batch update.");
        }

        if (--_batchDepth == 0)
        {
            if (_editorFormatMap.IsInBatchUpdate)
            {
                _editorFormatMap.EndBatchUpdate();
            }

            if (_pendingChange)
            {
                _pendingChange = false;
                ClassificationFormatMappingChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void RaiseChanged()
    {
        if (IsInBatchUpdate)
        {
            _pendingChange = true;
        }
        else
        {
            ClassificationFormatMappingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private TextFormattingRunProperties MergeLineage(IClassificationType classificationType, HashSet<IClassificationType> visited)
    {
        var properties = _keyByClassification.ContainsKey(classificationType.Classification)
            ? GetExplicitTextProperties(classificationType)
            : TextFormattingRunProperties.CreateTextFormattingRunProperties();

        if (visited.Add(classificationType))
        {
            foreach (var baseType in classificationType.BaseTypes)
            {
                properties = Merge(properties, MergeLineage(baseType, visited));
            }
        }

        return properties;
    }

    /// <summary>Fills the empty properties of <paramref name="target"/> from <paramref name="fallback"/>.</summary>
    private static TextFormattingRunProperties Merge(TextFormattingRunProperties target, TextFormattingRunProperties fallback)
    {
        if (target.ForegroundBrushEmpty && !fallback.ForegroundBrushEmpty)
        {
            target = target.SetForegroundBrush(fallback.ForegroundBrush);
        }

        if (target.BackgroundBrushEmpty && !fallback.BackgroundBrushEmpty && fallback.BackgroundBrush is { } background)
        {
            target = target.SetBackgroundBrush(background);
        }

        if (target.TypefaceEmpty && !fallback.TypefaceEmpty)
        {
            target = target.SetTypeface(fallback.Typeface);
        }

        if (target.FontRenderingEmSizeEmpty && !fallback.FontRenderingEmSizeEmpty)
        {
            target = target.SetFontRenderingEmSize(fallback.FontRenderingEmSize);
        }

        if (target.FontHintingEmSizeEmpty && !fallback.FontHintingEmSizeEmpty)
        {
            target = target.SetFontHintingEmSize(fallback.FontHintingEmSize);
        }

        if (target.BoldEmpty && !fallback.BoldEmpty)
        {
            target = target.SetBold(fallback.Bold);
        }

        if (target.ItalicEmpty && !fallback.ItalicEmpty)
        {
            target = target.SetItalic(fallback.Italic);
        }

        if (target.TextDecorationsEmpty && !fallback.TextDecorationsEmpty && fallback.TextDecorations is { } decorations)
        {
            target = target.SetTextDecorations(decorations);
        }

        if (target.ForegroundOpacityEmpty && !fallback.ForegroundOpacityEmpty)
        {
            target = target.SetForegroundOpacity(fallback.ForegroundOpacity);
        }

        if (target.BackgroundOpacityEmpty && !fallback.BackgroundOpacityEmpty)
        {
            target = target.SetBackgroundOpacity(fallback.BackgroundOpacity);
        }

        if (target.CultureInfoEmpty && !fallback.CultureInfoEmpty && fallback.CultureInfo is { } culture)
        {
            target = target.SetCultureInfo(culture);
        }

        return target;
    }

    private static TextFormattingRunProperties FromResourceDictionary(ResourceDictionary dictionary)
    {
        var properties = TextFormattingRunProperties.CreateTextFormattingRunProperties();
        if (Get<IBrush>(dictionary, EditorFormatDefinition.ForegroundBrushId) is { } foreground)
        {
            properties = properties.SetForegroundBrush(foreground);
        }
        else if (Get<Color?>(dictionary, EditorFormatDefinition.ForegroundColorId) is { } foregroundColor)
        {
            properties = properties.SetForeground(foregroundColor);
        }

        if (Get<IBrush>(dictionary, EditorFormatDefinition.BackgroundBrushId) is { } background)
        {
            properties = properties.SetBackgroundBrush(background);
        }
        else if (Get<Color?>(dictionary, EditorFormatDefinition.BackgroundColorId) is { } backgroundColor)
        {
            properties = properties.SetBackground(backgroundColor);
        }

        if (Get<object>(dictionary, ClassificationFormatDefinition.TypefaceId) is Typeface typeface)
        {
            properties = properties.SetTypeface(typeface);
        }

        if (Get<double?>(dictionary, ClassificationFormatDefinition.FontRenderingSizeId) is { } renderingSize)
        {
            properties = properties.SetFontRenderingEmSize(renderingSize);
        }

        if (Get<double?>(dictionary, ClassificationFormatDefinition.FontHintingSizeId) is { } hintingSize)
        {
            properties = properties.SetFontHintingEmSize(hintingSize);
        }

        if (Get<bool?>(dictionary, ClassificationFormatDefinition.IsBoldId) is { } isBold)
        {
            properties = properties.SetBold(isBold);
        }

        if (Get<bool?>(dictionary, ClassificationFormatDefinition.IsItalicId) is { } isItalic)
        {
            properties = properties.SetItalic(isItalic);
        }

        if (Get<TextDecorationCollection>(dictionary, ClassificationFormatDefinition.TextDecorationsId) is { } textDecorations)
        {
            properties = properties.SetTextDecorations(textDecorations);
        }

        if (Get<double?>(dictionary, ClassificationFormatDefinition.ForegroundOpacityId) is { } foregroundOpacity)
        {
            properties = properties.SetForegroundOpacity(foregroundOpacity);
        }

        if (Get<double?>(dictionary, ClassificationFormatDefinition.BackgroundOpacityId) is { } backgroundOpacity)
        {
            properties = properties.SetBackgroundOpacity(backgroundOpacity);
        }

        if (Get<CultureInfo>(dictionary, ClassificationFormatDefinition.CultureInfoId) is { } cultureInfo)
        {
            properties = properties.SetCultureInfo(cultureInfo);
        }

        return properties;
    }

    private static ResourceDictionary ToResourceDictionary(TextFormattingRunProperties properties)
    {
        var dictionary = new ResourceDictionary();
        if (!properties.ForegroundBrushEmpty)
        {
            dictionary[EditorFormatDefinition.ForegroundBrushId] = properties.ForegroundBrush;
        }

        if (!properties.BackgroundBrushEmpty)
        {
            dictionary[EditorFormatDefinition.BackgroundBrushId] = properties.BackgroundBrush;
        }

        if (!properties.TypefaceEmpty)
        {
            dictionary[ClassificationFormatDefinition.TypefaceId] = properties.Typeface;
        }

        if (!properties.FontRenderingEmSizeEmpty)
        {
            dictionary[ClassificationFormatDefinition.FontRenderingSizeId] = properties.FontRenderingEmSize;
        }

        if (!properties.FontHintingEmSizeEmpty)
        {
            dictionary[ClassificationFormatDefinition.FontHintingSizeId] = properties.FontHintingEmSize;
        }

        if (!properties.BoldEmpty)
        {
            dictionary[ClassificationFormatDefinition.IsBoldId] = properties.Bold;
        }

        if (!properties.ItalicEmpty)
        {
            dictionary[ClassificationFormatDefinition.IsItalicId] = properties.Italic;
        }

        if (!properties.TextDecorationsEmpty)
        {
            dictionary[ClassificationFormatDefinition.TextDecorationsId] = properties.TextDecorations;
        }

        if (!properties.ForegroundOpacityEmpty)
        {
            dictionary[ClassificationFormatDefinition.ForegroundOpacityId] = properties.ForegroundOpacity;
        }

        if (!properties.BackgroundOpacityEmpty)
        {
            dictionary[ClassificationFormatDefinition.BackgroundOpacityId] = properties.BackgroundOpacity;
        }

        if (!properties.CultureInfoEmpty)
        {
            dictionary[ClassificationFormatDefinition.CultureInfoId] = properties.CultureInfo;
        }

        return dictionary;
    }

    private static T? Get<T>(ResourceDictionary dictionary, string key)
        => dictionary.TryGetValue(key, out var value) && value is T typed ? typed : default;
}
