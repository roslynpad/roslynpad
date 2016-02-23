using System;
using Microsoft.CodeAnalysis;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.Diagnostics
{
    public class UpdatedEventArgs : EventArgs
    {
        public object Id { get; }

        public Workspace Workspace { get; }

        public ProjectId ProjectId { get; }

        public DocumentId DocumentId { get; }

        internal UpdatedEventArgs(object inner)
        {
            Id = inner.GetPropertyValue<object>(nameof(Id));
            Workspace = inner.GetPropertyValue<Workspace>(nameof(Workspace));
            ProjectId = inner.GetPropertyValue<ProjectId>(nameof(ProjectId));
            DocumentId = inner.GetPropertyValue<DocumentId>(nameof(DocumentId));
        }
    }
}