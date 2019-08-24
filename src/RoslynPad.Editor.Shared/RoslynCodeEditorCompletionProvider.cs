using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.SignatureHelp;
using RoslynPad.Roslyn.Snippets;

namespace RoslynPad.Editor
{
    public sealed class RoslynCodeEditorCompletionProvider : ICodeEditorCompletionProvider
    {
        private static bool _initialized;

        private readonly DocumentId _documentId;
        private readonly IRoslynHost _roslynHost;
        private readonly SnippetInfoService _snippetService;

        public RoslynCodeEditorCompletionProvider(DocumentId documentId, IRoslynHost roslynHost)
        {
            _documentId = documentId;
            _roslynHost = roslynHost;
            _snippetService = (SnippetInfoService)_roslynHost.GetService<ISnippetInfoService>();
        }

        // initialize the providers once in the app domain so typing would start faster
        internal void Warmup()
        {
            if (_initialized) return;

            _initialized = true;

            Task.Run(() =>
            {
                var document = _roslynHost.GetDocument(_documentId);
                if (document == null)
                {
                    return;
                }

                var completionService = CompletionService.GetService(document);
                completionService.GetCompletionsAsync(document, 0);

                var signatureHelpProvider = _roslynHost.GetService<ISignatureHelpProvider>();
                signatureHelpProvider.GetItemsAsync(document, 0,
                    new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand));
            });
        }

        public async Task<CompletionResult> GetCompletionData(int position, char? triggerChar, bool useSignatureHelp)
        {
            IList<ICompletionDataEx>? completionData = null;
            IOverloadProviderEx? overloadProvider = null;
            var useHardSelection = true;

            var document = _roslynHost.GetDocument(_documentId);
            if (document == null)
            {
                return new CompletionResult(null, null, false);
            }

            if (useSignatureHelp || triggerChar != null)
            {
                var signatureHelpProvider = _roslynHost.GetService<ISignatureHelpProvider>();
                var isSignatureHelp = useSignatureHelp || signatureHelpProvider.IsTriggerCharacter(triggerChar.GetValueOrDefault());
                if (isSignatureHelp)
                {
                    var signatureHelp = await signatureHelpProvider.GetItemsAsync(
                        document,
                        position,
                        new SignatureHelpTriggerInfo(
                            useSignatureHelp
                                ? SignatureHelpTriggerReason.InvokeSignatureHelpCommand
                                : SignatureHelpTriggerReason.TypeCharCommand, triggerChar))
                        .ConfigureAwait(false);
                    if (signatureHelp != null)
                    {
                        overloadProvider = new RoslynOverloadProvider(signatureHelp);
                    }
                }
            }

            if (overloadProvider == null)
            {
                var completionService = CompletionService.GetService(document);
                var completionTrigger = GetCompletionTrigger(triggerChar);
                var data = await completionService.GetCompletionsAsync(
                    document,
                    position,
                    completionTrigger
                    ).ConfigureAwait(false);
                if (data != null && data.Items.Any())
                {
                    useHardSelection = data.SuggestionModeItem == null;
                    var helper = CompletionHelper.GetHelper(document, completionService);
                    var text = await document.GetTextAsync().ConfigureAwait(false);
                    var textSpanToText = new Dictionary<TextSpan, string>();

                    completionData = data.Items
                        .Where(item => MatchesFilterText(helper, item, text, textSpanToText))
                        .Select(item => new RoslynCompletionData(document, item, triggerChar, _snippetService.SnippetManager))
                            .ToArray<ICompletionDataEx>();
                }
                else
                {
                    completionData = Array.Empty<ICompletionDataEx>();
                }
            }

            return new CompletionResult(completionData, overloadProvider, useHardSelection);
        }

        private static bool MatchesFilterText(CompletionHelper helper, CompletionItem item, SourceText text, Dictionary<TextSpan, string> textSpanToText)
        {
            var filterText = GetFilterText(item, text, textSpanToText);
            if (string.IsNullOrEmpty(filterText)) return true;
            return helper.MatchesFilterText(item, filterText);
        }

        private static string GetFilterText(CompletionItem item, SourceText text, Dictionary<TextSpan, string> textSpanToText)
        {
            var textSpan = item.Span;
            if (!textSpanToText.TryGetValue(textSpan, out var filterText))
            {
                filterText = text.GetSubText(textSpan).ToString();
                textSpanToText[textSpan] = filterText;
            }
            return filterText;
        }

        private static CompletionTrigger GetCompletionTrigger(char? triggerChar)
        {
            return triggerChar != null
                ? CompletionTrigger.CreateInsertionTrigger(triggerChar.Value)
                : CompletionTrigger.Invoke;
        }
    }
}