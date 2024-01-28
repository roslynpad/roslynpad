// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn;

public class RoslynWorkspace : Workspace
{
    public DocumentId? OpenDocumentId { get; private set; }
    public RoslynHost? RoslynHost { get; }

    public RoslynWorkspace(HostServices hostServices, string workspaceKind = WorkspaceKind.Host, RoslynHost? roslynHost = null)
        : base(hostServices, workspaceKind)
    {
        DiagnosticProvider.Enable(this);

        RoslynHost = roslynHost;
    }

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

        DiagnosticProvider.Disable(this);
    }

    protected override void ApplyDocumentTextChanged(DocumentId id, SourceText text)
    {
        if (OpenDocumentId != id)
        {
            return;
        }

        ApplyingTextChange?.Invoke(id, text);

        OnDocumentTextChanged(id, text, PreservationMode.PreserveIdentity);
    }
}
