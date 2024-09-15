using System.Collections.Concurrent;
using System.Composition;
using System.Runtime.ExceptionServices;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using RoslynPad.Roslyn.Completion.Providers;
using IPackageSourceProvider = NuGet.Configuration.IPackageSourceProvider;
using PackageSource = NuGet.Configuration.PackageSource;
using PackageSourceProvider = NuGet.Configuration.PackageSourceProvider;
using Settings = NuGet.Configuration.Settings;

namespace RoslynPad.UI;

[Export, Export(typeof(INuGetCompletionProvider)), Shared]
public sealed class NuGetViewModel : NotificationObject, INuGetCompletionProvider
{
    private const int MaxSearchResults = 50;

    private readonly CommandLineSourceRepositoryProvider? _sourceRepositoryProvider;
    private readonly ExceptionDispatchInfo? _initializationException;

    public string ConfigPath { get; set; }
    public string GlobalPackageFolder { get; }

    [ImportingConstructor]
    public NuGetViewModel([Import(AllowDefault = true)] ITelemetryProvider? telemetryProvider, IApplicationSettings appSettings)
    {
        try
        {
            var settings = LoadSettings();
            ConfigPath = settings.GetConfigFilePaths().First();
            GlobalPackageFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

            DefaultCredentialServiceUtility.SetupDefaultCredentialService(NullLogger.Instance, nonInteractive: false);

            var sourceProvider = new PackageSourceProvider(settings);
            _sourceRepositoryProvider = new CommandLineSourceRepositoryProvider(sourceProvider);
        }
        catch (Exception e)
        {
            _initializationException = ExceptionDispatchInfo.Capture(e);

            ConfigPath = string.Empty;
            GlobalPackageFolder = string.Empty;
        }

        Settings LoadSettings()
        {
            Settings? settings = null;

            const int retries = 3;

            for (var i = 1; i <= retries; i++)
            {

                try
                {
                    settings = new Settings(appSettings.GetDefaultDocumentPath(), "RoslynPad.nuget.config");
                }
                catch (NuGetConfigurationException ex)
                {
                    if (i == retries)
                    {
                        telemetryProvider?.ReportError(ex);
                        throw;
                    }
                }
            }

            return settings!;
        }
    }

    public async Task<IReadOnlyList<PackageData>> GetPackagesAsync(string searchTerm, bool includePrerelease, bool exactMatch, CancellationToken cancellationToken)
    {
        _initializationException?.Throw();

        var filter = new SearchFilter(includePrerelease);
        var packages = new List<PackageData>();

        foreach (var sourceRepository in _sourceRepositoryProvider!.GetRepositories())
        {
            IPackageSearchMetadata[]? result;
            try
            {
                result = await sourceRepository.SearchAsync(searchTerm, filter, MaxSearchResults, cancellationToken).ConfigureAwait(false);
            }
            catch (FatalProtocolException)
            {
                continue;
            }

            if (exactMatch)
            {
                var match = result.FirstOrDefault(c => string.Equals(c.Identity.Id, searchTerm,
                    StringComparison.OrdinalIgnoreCase));
                result = match != null ? [match] : null;
            }

            if (result?.Length > 0)
            {
                var repositoryPackages = result
                                         .Select(x => new PackageData(x))
                                         .ToArray();
                await Task.WhenAll(repositoryPackages.Select(x => x.Initialize())).ConfigureAwait(false);
                packages.AddRange(repositoryPackages);
            }
        }

        return packages;
    }

    async Task<IReadOnlyList<INuGetPackage>> INuGetCompletionProvider.SearchPackagesAsync(string searchString, bool exactMatch, CancellationToken cancellationToken)
    {
        var packages = await GetPackagesAsync(searchString, includePrerelease: true, exactMatch, cancellationToken).ConfigureAwait(false);
        return packages;
    }

    private class CommandLineSourceRepositoryProvider : ISourceRepositoryProvider
    {
        private readonly List<Lazy<INuGetResourceProvider>> _resourceProviders;
        private readonly List<SourceRepository> _repositories;

        // There should only be one instance of the source repository for each package source.
        private static readonly ConcurrentDictionary<PackageSource, SourceRepository> s_cachedSources
            = new();

        public CommandLineSourceRepositoryProvider(IPackageSourceProvider packageSourceProvider)
        {
            PackageSourceProvider = packageSourceProvider;

            _resourceProviders = [.. Repository.Provider.GetCoreV3()];

            // Create repositories
            _repositories = PackageSourceProvider.LoadPackageSources()
                .Where(s => s.IsEnabled)
                .Select(CreateRepository)
                .ToList();
        }

        public IEnumerable<SourceRepository> GetRepositories()
        {
            return _repositories;
        }

        public SourceRepository CreateRepository(PackageSource source)
        {
            return s_cachedSources.GetOrAdd(source, new SourceRepository(source, _resourceProviders));
        }

        public SourceRepository CreateRepository(PackageSource source, FeedType type)
        {
            return s_cachedSources.GetOrAdd(source, new SourceRepository(source, _resourceProviders, type));
        }

        public IPackageSourceProvider PackageSourceProvider { get; }
    }
}
