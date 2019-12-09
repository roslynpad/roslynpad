using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn
{
    public interface IRoslynHost
    {
        ParseOptions ParseOptions { get; }

        TService GetService<TService>();

        DocumentId AddDocument(DocumentCreationArgs args);

        Document? GetDocument(DocumentId documentId);

        void CloseDocument(DocumentId documentId);

        MetadataReference CreateMetadataReference(string location);
    }
}