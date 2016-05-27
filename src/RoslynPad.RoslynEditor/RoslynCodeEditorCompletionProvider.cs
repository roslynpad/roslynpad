using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.SignatureHelp;
using RoslynPad.Roslyn.Snippets;

namespace RoslynPad.RoslynEditor
{
    internal sealed class RoslynCodeEditorCompletionProvider : ICodeEditorCompletionProvider
    {
        private readonly DocumentId _documentId;
        private readonly RoslynHost _roslynHost;
        private readonly SnippetInfoService _snippetService;

        public RoslynCodeEditorCompletionProvider(DocumentId documentId, RoslynHost roslynHost)
        {
            _documentId = documentId;
            _roslynHost = roslynHost;
            _snippetService = (SnippetInfoService)_roslynHost.GetService<ISnippetInfoService>();
        }

        public async Task<CompletionResult> GetCompletionData(int position, char? triggerChar, bool useSignatureHelp)
        {
            IList<ICompletionDataEx> completionData = null;
            IOverloadProviderEx overloadProvider = null;
            bool? isCompletion = null;

            var document = _roslynHost.GetDocument(_documentId);

            if (useSignatureHelp || triggerChar != null)
            {
                var signatureHelpProvider = _roslynHost.GetService<ISignatureHelpProvider>();
                var isSignatureHelp = useSignatureHelp || signatureHelpProvider.IsTriggerCharacter(triggerChar.Value);
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
                else
                {
                    var text = await document.GetTextAsync().ConfigureAwait(false);
                    isCompletion = CompletionService.GetService(document)
                        .ShouldTriggerCompletion(text, position, GetCompletionTrigger(triggerChar));
                }
            }

            if (overloadProvider == null && isCompletion != false)
            {
                var data = await CompletionService.GetService(document).GetCompletionsAsync(
                    document,
                    position,
                    GetCompletionTrigger(triggerChar)
                    ).ConfigureAwait(false);
                completionData = data?.Items.Select(item => new RoslynCompletionData(document, item, triggerChar, _snippetService.SnippetManager)).ToArray<ICompletionDataEx>()
                    ?? Array.Empty<ICompletionDataEx>();
            }

            return new CompletionResult(completionData, overloadProvider);
        }

        private static CompletionTrigger GetCompletionTrigger(char? triggerChar)
        {
            return triggerChar != null
                ? CompletionTrigger.CreateInsertionTrigger(triggerChar.Value)
                : CompletionTrigger.Default;
        }
    }
}