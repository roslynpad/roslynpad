using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;

namespace RoslynPad.Roslyn.Completion
{
    public static class CompletionItemExtensions
    {
        private static readonly ImmutableDictionary<string, ImmutableDictionary<string, Glyph>> Dictionary = InitializeDictionary();

        private static ImmutableDictionary<string, ImmutableDictionary<string, Glyph>> InitializeDictionary()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, Glyph>>();
            foreach (var glyph in (Glyph[])Enum.GetValues(typeof(Glyph)))
            {
                var tags = GlyphTags.GetTags((Microsoft.CodeAnalysis.Glyph)glyph);
                if (tags.IsDefaultOrEmpty) continue;

                var firstTag = tags[0];
                var secondTag = tags.Length == 2 ? tags[1] : string.Empty;
                var inner = builder.GetValueOrDefault(firstTag);
                if (inner == null)
                {
                    inner = ImmutableDictionary<string, Glyph>.Empty.Add(secondTag, glyph);
                }

                builder[firstTag] = inner.SetItem(secondTag, glyph);
            }

            return builder.ToImmutable();
        }

        public static Glyph? GetGlyph(this CompletionItem completionItem)
        {
            var tags = completionItem.Tags;
            for (var index = 0; index < tags.Length; index++)
            {
                var tag = tags[index];
                var inner = Dictionary.GetValueOrDefault(tag);
                if (inner != null)
                {
                    Glyph glyph;
                    if (inner.TryGetValue(string.Empty, out glyph) ||
                        (index + 1 < tags.Length && inner.TryGetValue(tags[index + 1], out glyph)))
                    {
                        return glyph;
                    }
                }
            }

            return null;
        }

        public static CompletionDescription GetDescription(this CompletionItem completionItem)
        {
            return CommonCompletionItem.GetDescription(completionItem);
        }
    }
}