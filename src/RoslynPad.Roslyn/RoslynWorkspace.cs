// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn
{
    public sealed class RoslynWorkspace : Workspace
    {
        public RoslynHost RoslynHost { get; }
        public DocumentId OpenDocumentId { get; private set; }

        internal RoslynWorkspace(HostServices host, RoslynHost roslynHost)
            : base(host, WorkspaceKind.Host)
        {
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
            switch (feature)
            {
                case ApplyChangesKind.ChangeDocument:
                    return true;
                default:
                    return false;
            }
        }

        public void OpenDocument(DocumentId documentId, SourceTextContainer textContainer)
        {
            OpenDocumentId = documentId;
            OnDocumentOpened(documentId, textContainer);
            OnDocumentContextUpdated(documentId);
        }

        public event Action<DocumentId, SourceText> ApplyingTextChange;

        protected override void ApplyDocumentTextChanged(DocumentId document, SourceText newText)
        {
            if (OpenDocumentId != document)
            {
                return;
            }

            ApplyingTextChange?.Invoke(document, newText);
            
            OnDocumentTextChanged(document, newText, PreservationMode.PreserveIdentity);
        }

        public new void ClearSolution()
        {
            base.ClearSolution();
        }

        internal void ClearOpenDocument(DocumentId documentId)
        {
            base.ClearOpenDocument(documentId);
        }

        internal new void RegisterText(SourceTextContainer textContainer)
        {
            base.RegisterText(textContainer);
        }

        internal new void UnregisterText(SourceTextContainer textContainer)
        {
            base.UnregisterText(textContainer);
        }
    }
}
