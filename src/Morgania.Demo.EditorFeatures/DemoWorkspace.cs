using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Morgania.Demo.EditorFeatures;

/// <summary>
/// A minimal host workspace, modeled on RoslynPad's RoslynWorkspace. The open document is
/// backed directly by the editor's <see cref="ITextBuffer"/>: opening the document with the
/// buffer's own <see cref="SourceTextContainer"/> makes Roslyn track buffer edits
/// automatically, and workspace-applied changes (code fixes, formatting) are written back to
/// the buffer as minimal edits.
/// </summary>
internal sealed class DemoWorkspace(HostServices hostServices) : Workspace(hostServices, WorkspaceKind.Host)
{
    private ITextBuffer? _openBuffer;

    public override bool CanOpenDocuments => true;

    public override bool CanApplyChange(ApplyChangesKind feature) => feature is ApplyChangesKind.ChangeDocument;

    public void SetSolution(Solution solution)
    {
        var oldSolution = CurrentSolution;
        var newSolution = SetCurrentSolution(solution);
        RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
    }

    public void OpenDocumentInBuffer(DocumentId documentId, ITextBuffer buffer)
    {
        _openBuffer = buffer;
        OnDocumentOpened(documentId, buffer.AsTextContainer());
        OnDocumentContextUpdated(documentId);
    }

    protected override void ApplyDocumentTextChanged(DocumentId id, SourceText text)
    {
        if (_openBuffer is not { } buffer)
        {
            return;
        }

        // The open-document text tracking round-trips the buffer edit back into the solution,
        // so only the buffer needs updating here.
        var oldText = buffer.CurrentSnapshot.AsText();
        using var edit = buffer.CreateEdit();
        foreach (var change in text.GetTextChanges(oldText))
        {
            edit.Replace(change.Span.ToSpan(), change.NewText);
        }

        edit.Apply();
    }
}
