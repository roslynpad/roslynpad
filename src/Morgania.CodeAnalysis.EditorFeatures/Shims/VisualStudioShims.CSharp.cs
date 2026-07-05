// Shims for Visual Studio types that are referenced by recompiled Roslyn C# EditorFeatures
// source but are not part of the Morgania editor platform.

// Referenced by an unused using directive in StringCopyPaste/StringCopyPasteHelpers.cs.
namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue.VsdbgIntegration
{
    file sealed class Dummy;
}

// Stand-ins for the closed-source VS Copilot suggestions API. EventHookup uses the service only
// to dismiss and block gray-text proposals while its tooltip is visible; without Copilot the real
// service is absent and the code no-ops, so a null-returning stub is behaviorally identical.
namespace Microsoft.VisualStudio.Language.Suggestions
{
    using System.ComponentModel.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Threading;

    internal enum ReasonForDismiss
    {
        DismissedAfterBufferChange,
    }

    [Export(typeof(SuggestionServiceBase))]
    internal class SuggestionServiceBase
    {
        public virtual Task<IAsyncDisposable?> DismissAndBlockProposalsAsync(ITextView textView, ReasonForDismiss reason, CancellationToken cancellationToken)
            => Task.FromResult<IAsyncDisposable?>(null);
    }
}
