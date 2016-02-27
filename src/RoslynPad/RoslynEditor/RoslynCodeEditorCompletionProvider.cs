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
                var isSignatureHelp = useSignatureHelp || await _roslynHost.IsSignatureHelpTriggerCharacter(position - 1).ConfigureAwait(false);
                if (isSignatureHelp)
                {
                    var signatureHelp = await _roslynHost.GetSignatureHelp(
                        new SignatureHelpTriggerInfo(
                            useSignatureHelp
                                ? SignatureHelpTriggerReason.InvokeSignatureHelpCommand
                                : SignatureHelpTriggerReason.TypeCharCommand, triggerChar), position)
                        .ConfigureAwait(false);
                    if (signatureHelp != null)
                    {
                        overloadProvider = new RoslynOverloadProvider(signatureHelp);
                    }
                }
                else
                {
                    isCompletion = await _roslynHost.IsCompletionTriggerCharacter(position - 1).ConfigureAwait(false);
                }
            }

            if (overloadProvider == null && isCompletion != false)
            {
                var data = await _roslynHost.GetCompletion(
                    triggerChar != null
                        ? CompletionTriggerInfo.CreateTypeCharTriggerInfo(triggerChar.Value)
                        : CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(),
                    position).ConfigureAwait(false);
                completionData = data?.Items.Select(item => new AvalonEditCompletionData(item)).ToArray<ICompletionDataEx>() 
                    ?? (IList<ICompletionDataEx>) new List<ICompletionDataEx>();
            }

            return new CompletionResult(completionData, overloadProvider);
        }
    }
}