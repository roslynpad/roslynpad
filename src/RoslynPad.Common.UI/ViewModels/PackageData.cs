using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using RoslynPad.Roslyn.Completion.Providers;

namespace RoslynPad.UI;

public sealed class PackageData : INuGetPackage
{
    private readonly IPackageSearchMetadata? _package;

    private PackageData(string id, NuGetVersion version)
    {
        Id = id;
        Version = version;
    }

    public string Id { get; }
    public NuGetVersion Version { get; }
    public ImmutableArray<PackageData> OtherVersions { get; private set; }

    IEnumerable<string> INuGetPackage.Versions
    {
        get
        {
            if (!OtherVersions.IsDefaultOrEmpty)
            {
                var lastStable = OtherVersions.FirstOrDefault(v => !v.Version.IsPrerelease);
                if (lastStable != null)
                {
                    yield return lastStable.Version.ToString();
                }

                foreach (var version in OtherVersions)
                {
                    if (version != lastStable)
                    {
                        yield return version.Version.ToString();
                    }
                }
            }
        }
    }

    public PackageData(IPackageSearchMetadata package)
    {
        _package = package;
        Id = package.Identity.Id;
        Version = package.Identity.Version;
    }

    public async Task Initialize()
    {
        if (_package == null) return;
        var versions = await _package.GetVersionsAsync().ConfigureAwait(false);
        OtherVersions = [.. versions.Select(x => new PackageData(Id, x.Version)).OrderByDescending(x => x.Version)];
    }
}
