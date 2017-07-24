using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Repositories;
using NuGet.Resolver;
using NuGet.Versioning;
using RoslynPad.UI.NuGet;
using RoslynPad.Utilities;
using IPackageSourceProvider = NuGet.Configuration.IPackageSourceProvider;
using ISettings = NuGet.Configuration.ISettings;
using PackageSource = NuGet.Configuration.PackageSource;
using PackageSourceProvider = NuGet.Configuration.PackageSourceProvider;
using Settings = NuGet.Configuration.Settings;
using IMachineWideSettings = NuGet.Configuration.IMachineWideSettings;
using Strings = NuGet.Packaging.Strings;

namespace RoslynPad.UI
{
    [Export, Shared]
    public sealed class NuGetViewModel : NotificationObject
    {
        private const string TargetFrameworkName = "net46";
        private const string TargetFrameworkFullName = ".NET Framework, Version=4.6";
        private const int MaxSearchResults = 50;

        private readonly ISettings _settings;
        private readonly PackageSourceProvider _sourceProvider;
        private readonly IEnumerable<PackageSource> _packageSources;
        private readonly CommandLineSourceRepositoryProvider _sourceRepositoryProvider;
        private readonly ExceptionDispatchInfo _initializationException;

        public string GlobalPackageFolder { get; }

        public NuGetViewModel()
        {
            try
            {
                _settings = Settings.LoadDefaultSettings(
                    root: null,
                    configFileName: null,
                    machineWideSettings: new CommandLineMachineWideSettings());

                _sourceProvider = new PackageSourceProvider(_settings);

                GlobalPackageFolder = SettingsUtility.GetGlobalPackagesFolder(_settings);

                _packageSources = GetPackageSources();

                _sourceRepositoryProvider = new CommandLineSourceRepositoryProvider(_sourceProvider);
            }
            catch (Exception e)
            {
                _initializationException = ExceptionDispatchInfo.Capture(e);
            }
        }

        public async Task<IReadOnlyList<PackageData>> GetPackagesAsync(string searchTerm, bool includePrerelease, bool exactMatch, CancellationToken cancellationToken)
        {
            _initializationException?.Throw();

            return await GetPackages(searchTerm, includePrerelease, exactMatch, cancellationToken).ConfigureAwait(false);
        }

        internal static FrameworkSpecificGroup GetMostCompatibleGroup(NuGetFramework projectTargetFramework,
            IEnumerable<FrameworkSpecificGroup> itemGroups)
        {
            var reducer = new FrameworkReducer();
            var mostCompatibleFramework
                = reducer.GetNearest(projectTargetFramework, itemGroups.Select(i => i.TargetFramework));
            if (mostCompatibleFramework != null)
            {
                var mostCompatibleGroup
                    = itemGroups.FirstOrDefault(i => i.TargetFramework.Equals(mostCompatibleFramework));

                if (IsValid(mostCompatibleGroup))
                {
                    return mostCompatibleGroup;
                }
            }

            return null;
        }

        internal static FrameworkSpecificGroup Normalize(FrameworkSpecificGroup group)
        {
            // Default to returning the same group
            var result = group;

            // If the group is null or it does not contain any items besides _._ then this is a no-op.
            // If it does have items create a new normalized group to replace it with.
            if (group?.Items.Any() == true)
            {
                // Filter out invalid files
                var normalizedItems = GetValidPackageItems(group.Items)
                    .Select(ReplaceAltDirSeparatorWithDirSeparator);

                // Create a new group
                result = new FrameworkSpecificGroup(
                    targetFramework: group.TargetFramework,
                    items: normalizedItems);
            }

            return result;
        }

        public static string ReplaceAltDirSeparatorWithDirSeparator(string path)
        {
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        internal static IEnumerable<string> GetValidPackageItems(IEnumerable<string> items)
        {
            if (items == null
                || !items.Any())
            {
                return Enumerable.Empty<string>();
            }

            // Assume nupkg and nuspec as the save mode for identifying valid package files
            return items.Where(i => PackageHelper.IsPackageFile(i, PackageSaveMode.Defaultv3));
        }

        internal static bool IsValid(FrameworkSpecificGroup frameworkSpecificGroup)
        {
            if (frameworkSpecificGroup != null)
            {
                return (frameworkSpecificGroup.HasEmptyFolder
                        || frameworkSpecificGroup.Items.Any()
                        || !frameworkSpecificGroup.TargetFramework.Equals(NuGetFramework.AnyFramework));
            }

            return false;
        }

        private const string ResourceAssemblyExtension = ".resources.dll";
        private static readonly ImmutableArray<string> AssemblyReferencesExtensions
            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            = ImmutableArray<string>.Empty.Add(".dll").Add(".exe").Add(".winmd");


        private static bool IsAssemblyReference(string filePath)
        {
            // assembly reference must be under lib/
            if (!filePath.StartsWith(PackagingConstants.Folders.Lib + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !filePath.StartsWith(PackagingConstants.Folders.Lib + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileName = Path.GetFileName(filePath);

            // if it's an empty folder, yes
            if (fileName == PackagingCoreConstants.EmptyFolder)
            {
                return true;
            }

            // Assembly reference must have a .dll|.exe|.winmd extension and is not a resource assembly;
            return !filePath.EndsWith(ResourceAssemblyExtension, StringComparison.OrdinalIgnoreCase) &&
                   AssemblyReferencesExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
        }

        private IEnumerable<SourceRepository> GetEffectiveSources(IEnumerable<SourceRepository> primarySources, IEnumerable<SourceRepository> secondarySources)
        {
            // Always have to add the packages folder as the primary repository so that
            // dependency info for an installed package that is unlisted from the server is still available :(
            var effectiveSources = new List<SourceRepository>(primarySources);
            //effectiveSources.Add(PackagesFolderSourceRepository);
            effectiveSources.AddRange(secondarySources);

            return new HashSet<SourceRepository>(effectiveSources, new SourceRepositoryComparer());
        }

        private class SourceRepositoryComparer : IEqualityComparer<SourceRepository>
        {
            public bool Equals(SourceRepository x, SourceRepository y)
            {
                return x.PackageSource.Equals(y.PackageSource);
            }

            public int GetHashCode(SourceRepository obj)
            {
                return obj.PackageSource.GetHashCode();
            }
        }

        private async Task<IEnumerable<string>> PreviewInstallPackageAsync(NuGetFramework targetFramework, PackageIdentity packageIdentity,
            IEnumerable<SourceRepository> primarySources, IEnumerable<SourceRepository> secondarySources,
            bool includePrerelease,
            CancellationToken token)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            if (primarySources == null)
            {
                throw new ArgumentNullException(nameof(primarySources));
            }

            if (secondarySources == null)
            {
                secondarySources = _sourceRepositoryProvider.GetRepositories().Where(e => e.PackageSource.IsEnabled);
            }

            if (!primarySources.Any())
            {
                throw new ArgumentException(nameof(primarySources));
            }

            if (packageIdentity.Version == null)
            {
                throw new ArgumentNullException("packageIdentity.Version");
            }

            var nuGetProjectActions = new List<string>();

            var effectiveSources = GetEffectiveSources(primarySources, secondarySources);
            var downgradeAllowed = false;
            var packageTargetsForResolver = new HashSet<PackageIdentity>(PackageIdentity.Comparer);
            // Note: resolver needs all the installed packages as targets too. And, metadata should be gathered for the installed packages as well
            var installedPackageWithSameId =
                packageTargetsForResolver.FirstOrDefault(
                    p => p.Id.Equals(packageIdentity.Id, StringComparison.OrdinalIgnoreCase));
            if (installedPackageWithSameId != null)
            {
                packageTargetsForResolver.Remove(installedPackageWithSameId);
                if (installedPackageWithSameId.Version > packageIdentity.Version)
                {
                    // Looks like the installed package is of higher version than one being installed. So, we take it that downgrade is allowed
                    downgradeAllowed = true;
                }
            }
            packageTargetsForResolver.Add(packageIdentity);

            // Step-1 : Get metadata resources using gatherer
            var primaryPackages = new List<PackageIdentity> { packageIdentity };

            var gatherContext = new GatherContext
            {
                PrimaryTargets = primaryPackages,
                TargetFramework = targetFramework,
                PrimarySources = primarySources.ToList(),
                AllSources = effectiveSources.ToList(),
                PackagesFolderSource = _sourceRepositoryProvider.GetRepositories().First(),
                AllowDowngrades = downgradeAllowed,
            };

            var availablePackageDependencyInfoWithSourceSet = await ResolverGather.GatherAsync(gatherContext, token);

            if (!availablePackageDependencyInfoWithSourceSet.Any())
            {
                throw new InvalidOperationException("UnableToGatherDependencyInfo");
            }

            // Prune the results down to only what we would allow to be installed

            // Keep only the target package we are trying to install for that Id
            var prunedAvailablePackages =
                PrunePackageTree.RemoveAllVersionsForIdExcept(availablePackageDependencyInfoWithSourceSet,
                    packageIdentity);

            if (!downgradeAllowed)
            {
                prunedAvailablePackages =
                    PrunePackageTree.PruneDowngrades(prunedAvailablePackages, Enumerable.Empty<PackageReference>());
            }

            if (!includePrerelease)
            {
                prunedAvailablePackages = PrunePackageTree.PrunePreleaseForStableTargets(
                    prunedAvailablePackages,
                    packageTargetsForResolver,
                    new[] { packageIdentity });
            }

            // Remove versions that do not satisfy 'allowedVersions' attribute in packages.config, if any
            prunedAvailablePackages =
                PrunePackageTree.PruneDisallowedVersions(prunedAvailablePackages, Enumerable.Empty<PackageReference>());

            // Step-2 : Call PackageResolver.Resolve to get new list of installed packages

            // Note: resolver prefers installed package versions if the satisfy the dependency version constraints
            // So, since we want an exact version of a package, create a new list of installed packages where the packageIdentity being installed
            // is present after removing the one with the same id

            var packageResolverContext = new PackageResolverContext(DependencyBehavior.Highest,
                new[] { packageIdentity.Id },
                Enumerable.Empty<string>(),
                Enumerable.Empty<PackageReference>(),
                new[] { packageIdentity },
                prunedAvailablePackages,
                _sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
                NullLogger.Instance);

            var packageResolver = new PackageResolver();

            var newListOfInstalledPackages = packageResolver.Resolve(packageResolverContext, token);

            if (newListOfInstalledPackages == null)
            {
                throw new InvalidOperationException("UnableToResolveDependencyInfo");
            }

            // Step-3 : Get the list of nuGetProjectActions to perform, install/uninstall on the nugetproject
            // based on newPackages obtained in Step-2 and project.GetInstalledPackages

            foreach (var newPackageToInstall in newListOfInstalledPackages)
            {
                // find the package match based on identity
                var sourceDepInfo =
                    prunedAvailablePackages.SingleOrDefault(
                        p => PackageIdentity.Comparer.Equals(p, newPackageToInstall));

                if (sourceDepInfo == null)
                {
                    // this really should never happen
                    throw new InvalidOperationException("PackageNotFound:" + packageIdentity);
                }

                //nuGetProjectActions.Add(sourceDepInfo, sourceDepInfo.Source);
            }

            return nuGetProjectActions;
        }

        public async Task<bool> InstallPackageAsync(
            PackageIdentity packageIdentity,
            NuGetFramework targetFramework,
            DownloadResourceResult downloadResourceResult,
            CancellationToken token)
        {
            if (packageIdentity == null)
            {
                throw new ArgumentNullException(nameof(packageIdentity));
            }

            if (downloadResourceResult == null)
            {
                throw new ArgumentNullException(nameof(downloadResourceResult));
            }

            if (!downloadResourceResult.PackageStream.CanSeek)
            {
                throw new ArgumentException(Strings.PackageStreamShouldBeSeekable);
            }

            //// Step-1: Check if the package already exists after setting the nuGetProjectContext
            //SetNuGetProjectContext(nuGetProjectContext);

            //var packageReference = (await GetInstalledPackagesAsync(token))
            //    .FirstOrDefault(p => p.PackageIdentity.Equals(packageIdentity));
            //if (packageReference != null)
            //{
            //    return false;
            //}

            // Step-2: Create PackageArchiveReader using the PackageStream and obtain the various item groups
            downloadResourceResult.PackageStream.Seek(0, SeekOrigin.Begin);
            var packageReader = downloadResourceResult.PackageReader ?? new PackageArchiveReader(downloadResourceResult.PackageStream, leaveStreamOpen: true);

            var libItemGroups = packageReader.GetLibItems();
            var referenceItemGroups = packageReader.GetReferenceItems();
            var frameworkReferenceGroups = packageReader.GetFrameworkItems();
            var contentFileGroups = packageReader.GetContentItems();
            var buildFileGroups = packageReader.GetBuildItems();
            var toolItemGroups = packageReader.GetToolItems();

            // Step-3: Get the most compatible items groups for all items groups
            bool hasCompatibleProjectLevelContent;

            var compatibleLibItemsGroup = GetMostCompatibleGroup(targetFramework, libItemGroups);
            var compatibleReferenceItemsGroup = GetMostCompatibleGroup(targetFramework, referenceItemGroups);
            var compatibleFrameworkReferencesGroup = GetMostCompatibleGroup(targetFramework, frameworkReferenceGroups);
            var compatibleContentFilesGroup = GetMostCompatibleGroup(targetFramework, contentFileGroups);
            var compatibleBuildFilesGroup = GetMostCompatibleGroup(targetFramework, buildFileGroups);
            var compatibleToolItemsGroup = GetMostCompatibleGroup(targetFramework, toolItemGroups);

            compatibleLibItemsGroup = Normalize(compatibleLibItemsGroup);
            compatibleReferenceItemsGroup = Normalize(compatibleReferenceItemsGroup);
            compatibleFrameworkReferencesGroup = Normalize(compatibleFrameworkReferencesGroup);
            compatibleContentFilesGroup = Normalize(compatibleContentFilesGroup);
            compatibleBuildFilesGroup = Normalize(compatibleBuildFilesGroup);
            compatibleToolItemsGroup = Normalize(compatibleToolItemsGroup);

            hasCompatibleProjectLevelContent = IsValid(compatibleLibItemsGroup) ||
                                               IsValid(compatibleFrameworkReferencesGroup) ||
                                               IsValid(compatibleContentFilesGroup) ||
                                               IsValid(compatibleBuildFilesGroup);

            // Check if package has any content for project
            var hasProjectLevelContent = libItemGroups.Any() || frameworkReferenceGroups.Any()
                                         || contentFileGroups.Any() || buildFileGroups.Any();
            var onlyHasCompatibleTools = false;
            var onlyHasDependencies = false;

            if (!hasProjectLevelContent)
            {
                // Since it does not have project-level content, check if it has dependencies or compatible tools
                // Note that we are not checking if it has compatible project level content, but, just that it has project level content
                // If the package has project-level content, but nothing compatible, we still need to throw
                // If a package does not have any project-level content, it can be a
                // Legacy solution level packages which only has compatible tools group
                onlyHasCompatibleTools = IsValid(compatibleToolItemsGroup) && compatibleToolItemsGroup.Items.Any();
                if (!onlyHasCompatibleTools)
                {
                    // If it does not have compatible tool items either, check if it at least has dependencies
                    onlyHasDependencies = packageReader.GetPackageDependencies().Any();
                }
            }
            else
            {
                var shortFramework = targetFramework.GetShortFolderName();
            }

            // Step-6: Install package to FolderNuGetProject
            //await FolderNuGetProject.InstallPackageAsync(packageIdentity, downloadResourceResult, targetFramework, token);

            // Step-4: Check if there are any compatible items in the package or that this is not a package with only tools group. If not, throw
            if (!hasCompatibleProjectLevelContent
                && !onlyHasCompatibleTools
                && !onlyHasDependencies)
            {
                throw new InvalidOperationException(
                        $"UnableToFindCompatibleItems: {packageIdentity.Id} {packageIdentity.Version.ToNormalizedString()} {targetFramework}");
            }

            //var packageInstallPath = GetInstalledPath(packageIdentity);

            //// Step-8: MSBuildNuGetProjectSystem operations
            //// Step-8.1: Add references to project
            //if (IsValid(compatibleReferenceItemsGroup))
            //{
            //    foreach (var referenceItem in compatibleReferenceItemsGroup.Items)
            //    {
            //        if (IsAssemblyReference(referenceItem))
            //        {
            //            var referenceItemFullPath = Path.Combine(packageInstallPath, referenceItem);
            //            AddReference(referenceItemFullPath);
            //        }
            //    }
            //}

            //// Step-8.2: Add Frameworkreferences to project
            //if (IsValid(compatibleFrameworkReferencesGroup))
            //{
            //    foreach (var frameworkReference in compatibleFrameworkReferencesGroup.Items)
            //    {
            //        AddFrameworkReference(frameworkReference, packageIdentity.Id);
            //    }
            //}

            return true;
        }

        public async Task<NuGetInstallResult> InstallPackage(
            string packageId,
            NuGetVersion version,
            bool prerelease)
        {
            _initializationException?.Throw();

            //var installPath = Path.Combine(Path.GetTempPath(), "dummynuget");

            //var projectContext = new EmptyNuGetProjectContext
            //{
            //    PackageExtractionContext = new PackageExtractionContext(NullLogger.Instance)
            //};

            PackageIdentity currentIdentity = null;
            var references = new List<string>();
            var frameworkReferences = new List<string>();
            //var projectSystem = new DummyNuGetProjectSystem(projectContext,
            //    path => references.Add(GetPackagePath(currentIdentity, path)),
            //    path => frameworkReferences.Add(path));

            //var project = new MSBuildNuGetProject(projectSystem, installPath, installPath);
            //// this is a hack to get the identity of the package added in DummyNuGetProjectSystem.AddReference
            //project.PackageInstalling += (sender, args) => currentIdentity = args.Identity;
            //OverrideProject(project);

            //var packageManager = new NuGetPackageManager(_sourceRepositoryProvider, _settings, installPath);

            //var primaryRepositories = _packageSources.Select(_sourceRepositoryProvider.CreateRepository).ToArray();

            //var resolutionContext = new ResolutionContext(
            //    DependencyBehavior.Lowest,
            //    includePrelease: prerelease,
            //    includeUnlisted: true,
            //    versionConstraints: VersionConstraints.None);

            //if (version == null)
            //{
            //    // Find the latest version using NuGetPackageManager
            //    var resolvedPackage = await NuGetPackageManager.GetLatestVersionAsync(
            //        packageId,
            //        project,
            //        resolutionContext,
            //        primaryRepositories,
            //        NullLogger.Instance,
            //        CancellationToken.None).ConfigureAwait(false);

            //    if (resolvedPackage == null)
            //    {
            //        throw new Exception("Unable to find package");
            //    }

            //    version = resolvedPackage.LatestVersion;
            //}

            //var packageIdentity = new PackageIdentity(packageId, version);

            //await packageManager.InstallPackageAsync(
            //    project, 
            //    packageIdentity,
            //    resolutionContext,
            //    projectContext,
            //    primaryRepositories,
            //    Enumerable.Empty<SourceRepository>(),
            //    CancellationToken.None).ConfigureAwait(false);

            return new NuGetInstallResult(references.AsReadOnly(), frameworkReferences.AsReadOnly());
        }

        private static string GetPackagePath(PackageIdentity identity, string path)
        {
            return $@"{identity.Id}\{identity.Version.ToFullString()}\{path}";
        }

        private async Task<IReadOnlyList<PackageData>> GetPackages(string searchTerm, bool includePrerelease, bool exactMatch, CancellationToken cancellationToken)
        {
            var filter = new SearchFilter(includePrerelease)
            {
                SupportedFrameworks = new[] { TargetFrameworkFullName }
            };

            foreach (var sourceRepository in _sourceRepositoryProvider.GetRepositories())
            {
                IPackageSearchMetadata[] result;
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
                    result = match != null ? new[] { match } : null;
                }

                if (result?.Length > 0)
                {
                    var packages = result.Select(x => new PackageData(x)).ToArray();
                    await Task.WhenAll(packages.Select(x => x.Initialize())).ConfigureAwait(false);
                    return packages;
                }
            }

            return Array.Empty<PackageData>();
        }

        private IEnumerable<PackageSource> GetPackageSources()
        {
            var availableSources = _sourceProvider.LoadPackageSources().Where(source => source.IsEnabled);
            var packageSources = new List<PackageSource>();

            if (!string.IsNullOrEmpty(GlobalPackageFolder) && Directory.Exists(GlobalPackageFolder))
            {
                packageSources.Add(new PackageSource("Global", GlobalPackageFolder));
            }

            packageSources.AddRange(availableSources);

            return packageSources;
        }

        #region Inner Classes

        private class CommandLineSourceRepositoryProvider : ISourceRepositoryProvider
        {
            private readonly List<Lazy<INuGetResourceProvider>> _resourceProviders;
            private readonly List<SourceRepository> _repositories;

            // There should only be one instance of the source repository for each package source.
            private static readonly ConcurrentDictionary<PackageSource, SourceRepository> _cachedSources
                = new ConcurrentDictionary<PackageSource, SourceRepository>();

            public CommandLineSourceRepositoryProvider(IPackageSourceProvider packageSourceProvider)
            {
                PackageSourceProvider = packageSourceProvider;

                _resourceProviders = new List<Lazy<INuGetResourceProvider>>();
                _resourceProviders.AddRange(Repository.Provider.GetCoreV3());

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
                return _cachedSources.GetOrAdd(source, new SourceRepository(source, _resourceProviders));
            }

            public SourceRepository CreateRepository(PackageSource source, FeedType type)
            {
                return _cachedSources.GetOrAdd(source, new SourceRepository(source, _resourceProviders, type));
            }

            public IPackageSourceProvider PackageSourceProvider { get; }
        }

        private class CommandLineMachineWideSettings : IMachineWideSettings
        {
            private readonly Lazy<IEnumerable<Settings>> _settings;

            public CommandLineMachineWideSettings()
            {
                var baseDirectory = NuGetEnvironment.GetFolderPath(NuGetFolderPath.MachineWideConfigDirectory);
                _settings = new Lazy<IEnumerable<Settings>>(
                    () => global::NuGet.Configuration.Settings.LoadMachineWideSettings(baseDirectory));
            }

            public IEnumerable<Settings> Settings => _settings.Value;
        }

        #endregion
    }

    [Export]
    public sealed class NuGetDocumentViewModel : NotificationObject
    {
        private readonly NuGetViewModel _nuGetViewModel;
        private readonly ITelemetryProvider _telemetryProvider;

        private bool _isBusy;
        private bool _isEnabled;
        private string _searchTerm;
        private CancellationTokenSource _cts;
        private IReadOnlyList<PackageData> _packages;
        private bool _isPackagesMenuOpen;

        [ImportingConstructor]
        public NuGetDocumentViewModel(NuGetViewModel nuGetViewModel, ICommandProvider commands, ITelemetryProvider telemetryProvider)
        {
            _nuGetViewModel = nuGetViewModel;
            _telemetryProvider = telemetryProvider;

            InstallPackageCommand = commands.CreateAsync<PackageData>(InstallPackage);

            IsEnabled = true;
        }

        private Task InstallPackage(PackageData package)
        {
            return InstallPackage(package.Id, package.Version);
        }

        public async Task InstallPackage(string id, NuGetVersion version, bool reportInstalled = true)
        {
            IsBusy = true;
            IsEnabled = false;
            try
            {
                var result = await _nuGetViewModel.InstallPackage(id, version, prerelease: true).ConfigureAwait(false);

                if (reportInstalled)
                {
                    OnPackageInstalled(result);
                }
            }
            finally
            {
                IsBusy = false;
                IsEnabled = true;
            }
        }

        public IDelegateCommand<PackageData> InstallPackageCommand { get; }

        private void OnPackageInstalled(NuGetInstallResult result)
        {
            PackageInstalled?.Invoke(result);
        }

        public event Action<NuGetInstallResult> PackageInstalled;

        public bool IsBusy
        {
            get => _isBusy; private set => SetProperty(ref _isBusy, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled; private set => SetProperty(ref _isEnabled, value);
        }

        public string SearchTerm
        {
            get => _searchTerm; set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    PerformSearch(value, _cts.Token);
                }
            }
        }

        public IReadOnlyList<PackageData> Packages
        {
            get => _packages; private set => SetProperty(ref _packages, value);
        }

        public bool IsPackagesMenuOpen
        {
            get => _isPackagesMenuOpen;
            set => SetProperty(ref _isPackagesMenuOpen, value);
        }

        public bool ExactMatch { get; set; }

        private async void PerformSearch(string searchTerm, CancellationToken cancellationToken)
        {
            IsBusy = true;
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    IsPackagesMenuOpen = false;
                    Packages = null;
                    return;
                }
                try
                {
                    var packages = await Task.Run(() =>
                            _nuGetViewModel.GetPackagesAsync(searchTerm, includePrerelease: true,
                                exactMatch: ExactMatch, cancellationToken: cancellationToken), cancellationToken)
                        .ConfigureAwait(true);

                    Packages = packages;
                    IsPackagesMenuOpen = Packages.Count > 0;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    _telemetryProvider.ReportError(e);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    public sealed class PackageData
    {
        private readonly IPackageSearchMetadata _package;

        private PackageData(string id, NuGetVersion version)
        {
            Id = id;
            Version = version;
        }

        public string Id { get; }
        public NuGetVersion Version { get; }
        public ImmutableArray<PackageData> OtherVersions { get; private set; }

        //public PackageData(IList<IPackage> packages)
        //{
        //    var package = packages[0];
        //    Id = package.Id;
        //    Version = NuGetVersion.Parse(package.Version.ToString());
        //    OtherVersions = packages.Select(x => new PackageData(Id, NuGetVersion.Parse(x.Version.ToString()))).OrderByDescending(x => x.Version).ToImmutableArray();
        //}

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
            OtherVersions = versions.Select(x => new PackageData(Id, x.Version)).OrderByDescending(x => x.Version).ToImmutableArray();
        }
    }

    public sealed class NuGetInstallResult
    {
        public NuGetInstallResult(IReadOnlyList<string> references, IReadOnlyList<string> frameworkReferences)
        {
            References = references;
            FrameworkReferences = frameworkReferences;
        }

        public IReadOnlyList<string> References { get; }
        public IReadOnlyList<string> FrameworkReferences { get; }
    }

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

            return Array.Empty<IPackageSearchMetadata>();
        }
    }
}