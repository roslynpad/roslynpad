using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    public interface IRoslynHost
    {
        TService GetService<TService>();

        Document GetDocument(DocumentId documentId);

        void CloseDocument(DocumentId documentId);
    }
}