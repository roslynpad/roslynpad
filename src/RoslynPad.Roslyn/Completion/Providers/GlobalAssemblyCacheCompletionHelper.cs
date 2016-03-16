using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn.Completion.Providers
{
    internal sealed class GlobalAssemblyCacheCompletionHelper
    {
        private static readonly Lazy<List<string>> _lazyAssemblySimpleNames =
            new Lazy<List<string>>(() => GlobalAssemblyCache.Instance.GetAssemblySimpleNames().ToList());
        private readonly CompletionListProvider _completionProvider;
        private readonly TextSpan _textChangeSpan;
        private readonly Microsoft.CodeAnalysis.Completion.CompletionItemRules _itemRules;

        public GlobalAssemblyCacheCompletionHelper(CompletionListProvider completionProvider, TextSpan textChangeSpan, Microsoft.CodeAnalysis.Completion.CompletionItemRules itemRules = null)
        {
            _completionProvider = completionProvider;
            _textChangeSpan = textChangeSpan;
            _itemRules = itemRules;
        }

        public IEnumerable<Microsoft.CodeAnalysis.Completion.CompletionItem> GetItems(string pathSoFar, string documentPath)
        {
            var containsSlash = pathSoFar.Contains(@"/") || pathSoFar.Contains(@"\");
            if (containsSlash)
            {
                return SpecializedCollections.EmptyEnumerable<Microsoft.CodeAnalysis.Completion.CompletionItem>();
            }

            return GetCompletionsWorker(pathSoFar).ToList();
        }

        private IEnumerable<Microsoft.CodeAnalysis.Completion.CompletionItem> GetCompletionsWorker(string pathSoFar)
        {
            var comma = pathSoFar.IndexOf(',');
            if (comma >= 0)
            {
                var path = pathSoFar.Substring(0, comma);
                return from identity in GetAssemblyIdentities(path)
                    let text = identity.GetDisplayName()
                    select new Microsoft.CodeAnalysis.Completion.CompletionItem(_completionProvider, text, _textChangeSpan, glyph: Microsoft.CodeAnalysis.Glyph.Assembly, rules: _itemRules);
            }
            return from displayName in _lazyAssemblySimpleNames.Value
                select new Microsoft.CodeAnalysis.Completion.CompletionItem(
                    _completionProvider,
                    displayName, _textChangeSpan, c => Task.FromResult(GlobalAssemblyCache.Instance.ResolvePartialName(displayName).GetDisplayName().ToSymbolDisplayParts()), Microsoft.CodeAnalysis.Glyph.Assembly,
                    rules: _itemRules);
        }

        private static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string pathSoFar)
        {
            return IOUtilities.PerformIO(() => GlobalAssemblyCache.Instance.GetAssemblyIdentities(pathSoFar),
                SpecializedCollections.EmptyEnumerable<AssemblyIdentity>());
        }
    }
}