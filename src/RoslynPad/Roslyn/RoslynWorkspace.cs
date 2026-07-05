// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn;

public class RoslynWorkspace(HostServices hostServices, string workspaceKind = WorkspaceKind.Host, RoslynHost? roslynHost = null) : Workspace(hostServices, workspaceKind)
{
    public DocumentId? OpenDocumentId { get; private set; }
    public RoslynHost? RoslynHost { get; } = roslynHost;

    public new void SetCurrentSolution(Solution solution)
    {
        var oldSolution = CurrentSolution;
        var newSolution = base.SetCurrentSolution(solution);
        RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
    }

    public override bool CanOpenDocuments => true;

    public override bool CanApplyChange(ApplyChangesKind feature)
    {
        return feature switch
        {
            ApplyChangesKind.ChangeDocument or ApplyChangesKind.ChangeDocumentInfo or ApplyChangesKind.AddMetadataReference or ApplyChangesKind.RemoveMetadataReference or ApplyChangesKind.AddAnalyzerReference or ApplyChangesKind.RemoveAnalyzerReference => true,
            _ => false,
        };
    }

    public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
    {
        OpenDocumentId = documentId;
        OnDocumentOpened(documentId, textContainer);
        OnDocumentContextUpdated(documentId);
    }

    public event Action<DocumentId, SourceText>? ApplyingTextChange;

    protected override void Dispose(bool finalize)
    {
        base.Dispose(finalize);

        ApplyingTextChange = null;
    }

    protected override void ApplyDocumentTextChanged(DocumentId id, SourceText text)
    {
        if (OpenDocumentId != id)
        {
            return;
        }

        // When the document is open over an editor buffer, the handler writes the new text
        // into the buffer and the open-document tracking round-trips the edit back into the
        // solution; calling OnDocumentTextChanged here as well would apply it twice.
        if (ApplyingTextChange is { } applyingTextChange)
        {
            applyingTextChange.Invoke(id, text);
            return;
        }

        OnDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);
    }
}
