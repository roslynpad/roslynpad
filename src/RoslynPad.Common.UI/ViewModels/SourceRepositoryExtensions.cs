using NuGet.Common;
using NuGet.Protocol.Core.Types;

namespace RoslynPad.UI;

internal static class SourceRepositoryExtensions
{
    public static async Task<IPackageSearchMetadata[]> SearchAsync(this SourceRepository sourceRepository, string searchText, SearchFilter searchFilter, int pageSize, CancellationToken cancellationToken)
    {
        var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>(cancellationToken).ConfigureAwait(false);

        if (searchResource != null)
        {
            var searchResults = await searchResource.SearchAsync(
                searchText,
                searchFilter,
                0,
                pageSize,
                NullLogger.Instance,
                cancellationToken).ConfigureAwait(false);

            if (searchResults != null)
            {
                return searchResults.ToArray();
            }
        }

        return [];
    }
}
