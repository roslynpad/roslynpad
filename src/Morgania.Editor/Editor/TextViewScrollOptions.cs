#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using System.Composition;

using Microsoft.VisualStudio.Utilities;

/// <summary>
/// How far the text view scrolls, and when its scroll bars show. Ordinary editor options, so a host sets them globally,
/// on a buffer's options or on one view's (<see cref="IEditorOptions"/>).
/// </summary>
public static class TextViewScrollOptions
{
    /// <summary>
    /// Whether the view scrolls on past its last line until that line stands at the top of the viewport, as the Visual
    /// Studio editor does. Off stops with the last line at the viewport's bottom, so a document shorter than the viewport
    /// does not scroll at all. On by default.
    /// </summary>
    public const string ScrollBeyondLastLineOptionName = "TextView/ScrollBeyondLastLine";

    /// <summary>See <see cref="ScrollBeyondLastLineOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> ScrollBeyondLastLineId = new(ScrollBeyondLastLineOptionName);

    /// <summary>
    /// Whether a scroll bar shows only while there is something to scroll: the vertical one while the document does not
    /// stand whole in the viewport, the horizontal one while a line runs past it. Off by default, which shows a bar
    /// whenever its margin is enabled.
    /// </summary>
    public const string ScrollBarsOnlyWhenScrollableOptionName = "TextView/ScrollBarsOnlyWhenScrollable";

    /// <summary>See <see cref="ScrollBarsOnlyWhenScrollableOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> ScrollBarsOnlyWhenScrollableId = new(ScrollBarsOnlyWhenScrollableOptionName);
}

[Export(typeof(EditorOptionDefinition))]
[Name(TextViewScrollOptions.ScrollBeyondLastLineOptionName)]
[Shared]
public sealed class ScrollBeyondLastLine : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => TextViewScrollOptions.ScrollBeyondLastLineId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(TextViewScrollOptions.ScrollBarsOnlyWhenScrollableOptionName)]
[Shared]
public sealed class ScrollBarsOnlyWhenScrollable : EditorOptionDefinition<bool>
{
    public override bool Default => false;

    public override EditorOptionKey<bool> Key => TextViewScrollOptions.ScrollBarsOnlyWhenScrollableId;
}
