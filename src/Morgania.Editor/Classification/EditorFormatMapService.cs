#nullable enable

namespace Microsoft.VisualStudio.Text.Classification.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

[Export(typeof(IEditorFormatMapService))]
[Shared]
public sealed class EditorFormatMapService : IEditorFormatMapService
{
    private readonly Lazy<EditorFormatDefinition, EditorFormatMetadata>[] _definitions;
    private readonly Dictionary<string, IEditorFormatMap> _maps = new(StringComparer.OrdinalIgnoreCase);

    [ImportingConstructor]
    public EditorFormatMapService([ImportMany] Lazy<EditorFormatDefinition, EditorFormatMetadata>[] definitions)
    {
        _definitions = definitions;
    }

    public IEditorFormatMap GetEditorFormatMap(ITextView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return GetEditorFormatMap(view.Options.AppearanceCategory());
    }

    public IEditorFormatMap GetEditorFormatMap(string category)
    {
        ArgumentNullException.ThrowIfNull(category);
        lock (_maps)
        {
            if (!_maps.TryGetValue(category, out var map))
            {
                map = new EditorFormatMap(_definitions);
                _maps[category] = map;
            }

            return map;
        }
    }
}
