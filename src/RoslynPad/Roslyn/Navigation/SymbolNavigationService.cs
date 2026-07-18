using System.Composition;
using Avalonia.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.Navigation;

/// <summary>
/// Navigates to symbols without source: generates a metadata-as-source file (Source Link /
/// embedded PDB sources when available, otherwise decompilation) and asks the app to show it
/// in a read-only tab.
/// </summary>
[ExportWorkspaceService(typeof(ISymbolNavigationService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
internal sealed class SymbolNavigationService(NavigationBridge bridge, IMetadataAsSourceFileService metadataAsSourceFileService) : ISymbolNavigationService
{
    public async Task<INavigableLocation?> GetNavigableLocationAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
    {
        if (bridge.Host is not { } host)
        {
            return null;
        }

        var file = await metadataAsSourceFileService.GetGeneratedFileAsync(
            project.Solution.Workspace, project, symbol, signaturesOnly: false, MetadataAsSourceOptions.Default, cancellationToken).ConfigureAwait(false);

        return new NavigableLocation((_, ct) =>
            Dispatcher.UIThread.InvokeAsync(() => host.OpenMetadataAsSourceAsync(file, ct)));
    }

    public Task<bool> TrySymbolNavigationNotifyAsync(ISymbol symbol, Project project, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<(string filePath, LinePosition linePosition)?> GetExternalNavigationSymbolLocationAsync(DefinitionItem definitionItem, CancellationToken cancellationToken) =>
        Task.FromResult<(string filePath, LinePosition linePosition)?>(null);
}
