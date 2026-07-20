#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using Microsoft.VisualStudio.Text.Classification;

/// <summary>
/// The editor-format-map keys the selection layer reads its fill colors from (the VS Fonts
/// and Colors item names). Hosts theme the selection by setting
/// <see cref="EditorFormatDefinition.BackgroundBrushId"/> (or
/// <see cref="EditorFormatDefinition.BackgroundColorId"/>) on these entries; entries left
/// unset fall back to the built-in dark palette.
/// </summary>
public static class SelectionFormatNames
{
    public const string Active = "Selected Text";
    public const string Inactive = "Inactive Selected Text";
}
