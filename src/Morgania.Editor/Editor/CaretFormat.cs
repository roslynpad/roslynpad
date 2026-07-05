#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using Microsoft.VisualStudio.Text.Classification;

/// <summary>
/// The editor-format-map keys the caret layer reads its colors from (the VS Fonts and Colors
/// item names). Hosts theme the caret by setting <see cref="EditorFormatDefinition.ForegroundBrushId"/>
/// (or <see cref="EditorFormatDefinition.ForegroundColorId"/>) on these entries; entries left
/// unset fall back to the built-in dark palette, and a secondary entry left unset derives
/// from the primary color, dimmed.
/// </summary>
public static class CaretFormatNames
{
    public const string Primary = "Caret (Primary)";
    public const string Secondary = "Caret (Secondary)";
}
