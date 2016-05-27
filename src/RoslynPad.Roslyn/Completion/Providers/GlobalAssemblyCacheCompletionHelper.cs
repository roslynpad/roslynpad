using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace RoslynPad.Roslyn.Completion.Providers
{
    internal sealed class GlobalAssemblyCacheCompletionHelper
    {
        private static readonly Lazy<List<string>> _lazyAssemblySimpleNames =
            new Lazy<List<string>>(() => GlobalAssemblyCache.Instance.GetAssemblySimpleNames().ToList());
        private readonly TextSpan _textChangeSpan;
        private readonly CompletionItemRules _itemRules;

        public GlobalAssemblyCacheCompletionHelper(TextSpan textChangeSpan, CompletionItemRules itemRules = null)
        {
            _textChangeSpan = textChangeSpan;
            _itemRules = itemRules;
        }

        public IEnumerable<CompletionItem> GetItems(string pathSoFar, string documentPath)
        {
            var containsSlash = pathSoFar.Contains(@"/") || pathSoFar.Contains(@"\");
            if (containsSlash)
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            return GetCompletionsWorker(pathSoFar).ToList();
        }

        private IEnumerable<CompletionItem> GetCompletionsWorker(string pathSoFar)
        {
            var comma = pathSoFar.IndexOf(',');
            if (comma >= 0)
            {
                var path = pathSoFar.Substring(0, comma);
                return from identity in GetAssemblyIdentities(path)
                    let text = identity.GetDisplayName()
                    select CompletionItem.Create(text, span: _textChangeSpan, rules: _itemRules);
            }

            return from displayName in _lazyAssemblySimpleNames.Value
                select CommonCompletionItem.Create(
                    displayName, 
                    description: GlobalAssemblyCache.Instance.ResolvePartialName(displayName).GetDisplayName().ToSymbolDisplayParts(),
                    glyph: Microsoft.CodeAnalysis.Glyph.Assembly, 
                    span: _textChangeSpan,
                    rules: _itemRules);
        }

        private static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string pathSoFar)
        {
            return IOUtilities.PerformIO(() => GlobalAssemblyCache.Instance.GetAssemblyIdentities(pathSoFar),
                SpecializedCollections.EmptyEnumerable<AssemblyIdentity>());
        }
    }
}