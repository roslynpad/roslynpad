using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn.Navigation;

/// <summary>
/// Opens web links from editor features (e.g. the diagnostic-id hyperlink on inline
/// diagnostics) in the default browser; the default service navigates nowhere.
/// </summary>
[ExportWorkspaceService(typeof(INavigateToLinkService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
internal sealed class NavigateToLinkService() : INavigateToLinkService
{
    public Task<bool> TryNavigateToLinkAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Task.FromResult(false);
        }

        _ = Task.Run(() => Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }), CancellationToken.None);
        return Task.FromResult(true);
    }
}
