#nullable enable

namespace Microsoft.VisualStudio.Text.Classification.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

[Export(typeof(IClassificationFormatMapService))]
[Shared]
public sealed class ClassificationFormatMapService : IClassificationFormatMapService
{
    private readonly IEditorFormatMapService _editorFormatMapService;
    private readonly IClassificationTypeRegistryService _classificationTypeRegistry;
    private readonly IList<Lazy<EditorFormatDefinition, ClassificationFormatMetadata>> _orderedDefinitions;
    private readonly Dictionary<string, IClassificationFormatMap> _maps = new(StringComparer.OrdinalIgnoreCase);

    [ImportingConstructor]
    public ClassificationFormatMapService(
        IEditorFormatMapService editorFormatMapService,
        IClassificationTypeRegistryService classificationTypeRegistry,
        [ImportMany] Lazy<EditorFormatDefinition, ClassificationFormatMetadata>[] formatDefinitions)
    {
        ArgumentNullException.ThrowIfNull(formatDefinitions);
        _editorFormatMapService = editorFormatMapService;
        _classificationTypeRegistry = classificationTypeRegistry;

        // Only definitions carrying [ClassificationType] metadata participate; order them by
        // their [Order] attributes (priority order, lowest first).
        var classificationDefinitions = formatDefinitions
            .Where(definition => definition.Metadata.ClassificationTypeNames?.Any() == true)
            .ToList();
        _orderedDefinitions = Orderer.Order(classificationDefinitions);
    }

    public IClassificationFormatMap GetClassificationFormatMap(ITextView textView)
    {
        ArgumentNullException.ThrowIfNull(textView);
        return GetClassificationFormatMap(textView.Options.AppearanceCategory());
    }

    public IClassificationFormatMap GetClassificationFormatMap(string category)
    {
        ArgumentNullException.ThrowIfNull(category);
        lock (_maps)
        {
            if (!_maps.TryGetValue(category, out var map))
            {
                map = new ClassificationFormatMap(
                    _editorFormatMapService.GetEditorFormatMap(category),
                    _classificationTypeRegistry,
                    _orderedDefinitions);
                _maps[category] = map;
            }

            return map;
        }
    }
}
