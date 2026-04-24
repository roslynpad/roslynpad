using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Classification;

public static class TaggedTextResources
{
    public static string GetResourceKey(string textTag) => $"{nameof(TaggedText)}.{textTag}";

    public static IReadOnlyList<string> AllTags { get; } =
        [.. Enum.GetValues<SymbolDisplayPartKind>()
            .Select(SymbolDisplayPartKindTags.GetTag)];
}
