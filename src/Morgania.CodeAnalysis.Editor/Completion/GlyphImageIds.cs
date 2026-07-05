using System.Reflection;

namespace Morgania.CodeAnalysis.Editor.Completion;

/// <summary>
/// Exposes Roslyn's glyph → VS image-catalog mapping (the identifiers Roslyn puts in
/// completion item icons and quick info image elements).
/// </summary>
public static class GlyphImageIds
{
    private static readonly Lazy<IReadOnlyDictionary<int, string>> s_names = new(CreateNames);

    /// <summary>
    /// The image-catalog identifiers Roslyn emits, keyed to their moniker names — reflected
    /// from the constants of Roslyn's <c>Extensions.KnownImageIds</c>, whose names are the
    /// image-catalog monikers.
    /// </summary>
    public static IReadOnlyDictionary<int, string> ImageNames => s_names.Value;

    /// <summary>
    /// Gets the image-catalog moniker name Roslyn uses for the glyph, or null for glyphs
    /// Roslyn does not map to the catalog (e.g. <c>Glyph.AddReference</c>).
    /// </summary>
    internal static string? GetImageName(Microsoft.CodeAnalysis.Glyph glyph)
    {
        try
        {
            var (catalogGuid, id) = Microsoft.CodeAnalysis.LanguageServer.Extensions.GetVsImageData(glyph);
            return catalogGuid != Guid.Empty && s_names.Value.TryGetValue(id, out var name) ? name : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static Dictionary<int, string> CreateNames()
    {
        // The class is private in Roslyn, so this fails loudly on a Roslyn upgrade that
        // moves it — preferable to silently losing all glyph imagery.
        var knownImageIds = typeof(Microsoft.CodeAnalysis.LanguageServer.Extensions)
            .GetNestedType("KnownImageIds", BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMemberException("Microsoft.CodeAnalysis.LanguageServer.Extensions", "KnownImageIds");

        var names = new Dictionary<int, string>();
        foreach (var field in knownImageIds.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (field.IsLiteral && field.FieldType == typeof(int))
            {
                names[(int)field.GetRawConstantValue()!] = field.Name;
            }
        }

        return names;
    }
}
