using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.CodeCompletion;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.SignatureHelp;

namespace RoslynPad.RoslynEditor
{
    internal sealed class RoslynCodeEditorCompletionProvider : ICodeEditorCompletionProvider
    {
        private readonly RoslynHost _roslynHost;

        public RoslynCodeEditorCompletionProvider(RoslynHost roslynHost)
        {
            _roslynHost = roslynHost;
        }

        public async Task<CompletionResult> GetCompletionData(int position, char? triggerChar, bool useSignatureHelp)
        {
            IList<ICompletionDataEx> completionData = null;
            IOverloadProvider overloadProvider = null;
            bool? isCompletion = null;

            if (useSignatureHelp || triggerChar != null)
            {
                var signatureHelpProvider = _roslynHost.GetService<ISignatureHelpProvider>();
                var isSignatureHelp = useSignatureHelp || signatureHelpProvider.IsTriggerCharacter(triggerChar.Value);
                if (isSignatureHelp)
                {
                    var signatureHelp = await signatureHelpProvider.GetItemsAsync(
                        _roslynHost.CurrentDocument,
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
                    isCompletion = await CompletionService.IsCompletionTriggerCharacterAsync(_roslynHost.CurrentDocument, position - 1).ConfigureAwait(false);
                }
            }

            if (overloadProvider == null && isCompletion != false)
            {
                var data = await CompletionService.GetCompletionListAsync(
                    _roslynHost.CurrentDocument,
                    position,
                    triggerChar != null
                        ? CompletionTriggerInfo.CreateTypeCharTriggerInfo(triggerChar.Value)
                        : CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo()
                    ).ConfigureAwait(false);
                completionData = data?.Items.Select(item => new RoslynCompletionData(item)).ToArray<ICompletionDataEx>() 
                    ?? (IList<ICompletionDataEx>) new List<ICompletionDataEx>();
            }

            return new CompletionResult(completionData, overloadProvider);
        }
    }
}