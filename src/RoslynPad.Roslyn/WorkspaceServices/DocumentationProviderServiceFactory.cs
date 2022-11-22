using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.WorkspaceServices;

[ExportWorkspaceServiceFactory(typeof(IDocumentationProviderService), ServiceLayer.Host), Shared]
internal sealed class DocumentationProviderServiceFactory : IWorkspaceServiceFactory
{
    private readonly IDocumentationProviderService _service;

    [ImportingConstructor]
    public DocumentationProviderServiceFactory(IDocumentationProviderService service) => _service = service;

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices) => _service;
}

[Export(typeof(IDocumentationProviderService)), Shared]
internal sealed class DocumentationProviderService : IDocumentationProviderService
{
    private readonly ConcurrentDictionary<string, DocumentationProvider?> _assemblyPathToDocumentationProviderMap = new();

    public DocumentationProvider? GetDocumentationProvider(string location)
    {
        string? finalPath = Path.ChangeExtension(location, "xml");

        return _assemblyPathToDocumentationProviderMap.GetOrAdd(location, _ =>
        {
            if (!File.Exists(finalPath))
            {
                return null;
            }

            return XmlDocumentationProvider.CreateFromFile(finalPath);
        });
    }
}
