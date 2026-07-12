// Stub for the excluded vendor file DocumentationComments/CopilotGenerateDocumentationCommentManager.cs,
// which depends on the closed-source VS Copilot suggestion APIs (Microsoft.VisualStudio.Language.Suggestions).
// The real implementation no-ops when the suggestion service is unavailable, so a no-op stub is
// behaviorally identical and lets AbstractDocumentationCommentCommandHandler compile unmodified.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.DocumentationComments;

[Export(typeof(CopilotGenerateDocumentationCommentManager))]
internal sealed class CopilotGenerateDocumentationCommentManager
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CopilotGenerateDocumentationCommentManager()
    {
    }

    public void StartSuggestionSession(ITextBuffer subjectBuffer, ITextView textView, CancellationToken cancellationToken)
    {
    }

    public void TriggerDocumentationCommentProposalGeneration(Document document,
        DocumentationCommentSnippet snippet, ITextSnapshot snapshot, VirtualSnapshotPoint caret, ITextView textView, CancellationToken cancellationToken)
    {
    }
}
