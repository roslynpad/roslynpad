using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Classification;

public static class TaggedTextExtensions
{
    public static string ToClassificationTypeName(string taggedTextTag) =>
        Microsoft.CodeAnalysis.TaggedTextExtensions.ToClassificationTypeName(taggedTextTag);
 
    public static TaggedTextStyle GetStyle(this TaggedText taggedText) => (TaggedTextStyle)taggedText.Style;
}
