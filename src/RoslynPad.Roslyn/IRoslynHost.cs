using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    public interface IRoslynHost
    {
        TService GetService<TService>();

        DocumentId AddDocument(DocumentCreationArgs args);

        Document GetDocument(DocumentId documentId);

        void CloseDocument(DocumentId documentId);
    }
}