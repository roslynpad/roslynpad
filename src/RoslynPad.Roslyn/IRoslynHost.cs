using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;

namespace RoslynPad.Roslyn
{
    public interface IRoslynHost
    {
        TService GetService<TService>();

        DocumentId AddDocument(SourceTextContainer sourceTextContainer, string workingDirectory, Action<DiagnosticsUpdatedArgs> onDiagnosticsUpdated, Action<SourceText> onTextUpdated);

        Document GetDocument(DocumentId documentId);

        void CloseDocument(DocumentId documentId);
    }
}