using Avalonia.Media;
using Morgania.CodeAnalysis.Editor.Completion;
using Morgania.CodeAnalysis.Editor.Resources;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Resolves Visual Studio image-catalog identifiers to the glyph drawings embedded in
/// Glyphs.axaml (generated from the VS 2026 Image Library, keyed by moniker name).
/// The library icons are drawn for light backgrounds; when <see cref="ThemeBackground"/>
/// is set, colors are adapted with VS's image theming luminosity transform.
/// </summary>
public static class ImageCatalog
{
    /// <summary>The identifier of the Visual Studio image catalog.</summary>
    public static readonly Guid ImageCatalogGuid = Guid.Parse("ae27a6b0-e345-4288-96df-5eaf394ee369");

    private static readonly Glyphs s_drawings = [];
    private static readonly Dictionary<string, DrawingImage?> s_images = new(StringComparer.OrdinalIgnoreCase);
    private static Color? s_themeBackground;

    /// <summary>
    /// The image-catalog identifiers RoslynPad consumes, mapped to their moniker names:
    /// Roslyn's own id → moniker table (<see cref="GlyphImageIds.ImageNames"/>) plus ids
    /// Roslyn reaches without a Glyph.
    /// </summary>
    private static readonly Dictionary<int, string> s_names = CreateNames();

    private static Dictionary<int, string> CreateNames()
    {
        var names = new Dictionary<int, string>(GlyphImageIds.ImageNames);

        // The completion expander ("items from unimported namespaces"): Roslyn emits it
        // from the editor's KnownImageIds.ExpandScope rather than through a Glyph.
        names.TryAdd(1275, "ExpandScope");
        return names;
    }

    /// <summary>
    /// The background the icons render over; icon colors are adapted to it. Set by the host
    /// when the theme changes (null leaves the light-background colors untouched).
    /// </summary>
    public static Color? ThemeBackground
    {
        get => s_themeBackground;
        set
        {
            if (s_themeBackground != value)
            {
                s_themeBackground = value;
                s_images.Clear();
            }
        }
    }

    /// <summary>Gets the themed image for an image-catalog moniker name, or null if unknown.</summary>
    public static DrawingImage? GetImage(string name)
    {
        if (s_images.TryGetValue(name, out var image))
        {
            return image;
        }

        image = s_drawings.TryGetValue(name, out var resource) && resource is Drawing drawing
            ? new DrawingImage
            {
                Drawing = s_themeBackground is { } background
                    ? ImageThemingUtilities.TransformDrawing(drawing, background)
                    : drawing,
            }
            : null;
        s_images[name] = image;
        return image;
    }

    /// <summary>Gets the themed image for an image-catalog id, or null if unknown.</summary>
    public static DrawingImage? GetImage(Guid catalogGuid, int id) =>
        catalogGuid == ImageCatalogGuid && s_names.TryGetValue(id, out var name) ? GetImage(name) : null;
}
