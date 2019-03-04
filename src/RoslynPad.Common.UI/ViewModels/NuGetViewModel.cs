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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Credentials;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using RoslynPad.Roslyn.Completion.Providers;
using RoslynPad.Utilities;
using IPackageSourceProvider = NuGet.Configuration.IPackageSourceProvider;
using PackageSource = NuGet.Configuration.PackageSource;
using PackageSourceProvider = NuGet.Configuration.PackageSourceProvider;
using Settings = NuGet.Configuration.Settings;

namespace RoslynPad.UI
{
    [Export, Export(typeof(INuGetCompletionProvider)), Shared]
    public sealed class NuGetViewModel : NotificationObject, INuGetCompletionProvider
    {
        private const int MaxSearchResults = 50;

        private readonly CommandLineSourceRepositoryProvider _sourceRepositoryProvider;
        private readonly ExceptionDispatchInfo _initializationException;
        private readonly IEnumerable<string> _configFilePaths;
        private readonly IEnumerable<PackageSource> _packageSources;

        public string GlobalPackageFolder { get; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public NuGetViewModel()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            try
            {
                var settings = Settings.LoadDefaultSettings(
                    root: null,
                    configFileName: null,
                    machineWideSettings: new XPlatMachineWideSetting());

                GlobalPackageFolder = SettingsUtility.GetGlobalPackagesFolder(settings);
                _configFilePaths = SettingsUtility.GetConfigFilePaths(settings);
                _packageSources = SettingsUtility.GetEnabledSources(settings);

                DefaultCredentialServiceUtility.SetupDefaultCredentialService(NullLogger.Instance, nonInteractive: false);

                var sourceProvider = new PackageSourceProvider(settings);
                _sourceRepositoryProvider = new CommandLineSourceRepositoryProvider(sourceProvider);
            }
            catch (Exception e)
            {
                _initializationException = ExceptionDispatchInfo.Capture(e);
            }
        }

        public async Task<IReadOnlyList<PackageData>> GetPackagesAsync(string searchTerm, bool includePrerelease, bool exactMatch, CancellationToken cancellationToken)
        {
            _initializationException?.Throw();

            var filter = new SearchFilter(includePrerelease);

            foreach (var sourceRepository in _sourceRepositoryProvider.GetRepositories())
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

        internal static (List<string> compile, List<string> runtime) ReadProjectLockJson(JObject obj, string packagesDirectory, string framework)
        {
            var compile = new List<string>();
            var runtime = new List<string>();

            var targets = (JObject)obj["targets"];
            foreach (var target in targets)
            {
                if (target.Key == framework)
                {
                    foreach (var package in (JObject)target.Value)
                    {
                        var packageRoot = Path.Combine(packagesDirectory, package.Key);
                        ReadLockFileSection(packageRoot, package.Value, compile, nameof(compile));
                        ReadLockFileSection(packageRoot, package.Value, runtime, nameof(runtime));
                    }

                    break;
                }
            }

            return (compile, runtime);
        }

        private static void ReadLockFileSection(string packageRoot, JToken root, List<string> items, string sectionName)
        {
            var section = (JObject)((JObject)root)[sectionName];
            if (section == null)
            {
                return;
            }

            foreach (var item in section)
            {
                var relativePath = item.Key;
                // Ignore placeholder "_._" files.
                if (IsPlaceholder(relativePath))
                {
                    continue;
                }

                items.Add(Path.Combine(packageRoot, relativePath));
            }
        }

        internal static bool IsPlaceholder(string relativePath)
        {
            var name = Path.GetFileName(relativePath);
            bool isPlacehlder = string.Equals(name, "_._", StringComparison.InvariantCulture);
            return isPlacehlder;
        }

        internal static JObject LoadJson(TextReader reader)
        {
            JObject obj;

            using (var jsonReader = new JsonTextReader(reader))
            {
                obj = JObject.Load(jsonReader);
            }

            return obj;
        }

        internal RestoreParams CreateRestoreParams()
        {
            var restoreParams = new RestoreParams();

            foreach (var packageSource in _packageSources)
            {
                restoreParams.Sources.Add(packageSource);
            }

            foreach (var configFile in _configFilePaths)
            {
                restoreParams.ConfigFilePaths.Add(configFile);
            }

            restoreParams.PackagesPath = GlobalPackageFolder;

            return restoreParams;
        }

        internal static async Task<RestoreResult> RestoreAsync(RestoreParams restoreParameters, ILogger logger, CancellationToken cancellationToken = default)
        {
            var providerCache = new RestoreCommandProvidersCache();

            using (var cacheContext = new SourceCacheContext())
            {
                cacheContext.NoCache = false;
                cacheContext.IgnoreFailedSources = true;

                var providers = new List<IPreLoadedRestoreRequestProvider>();

                var dgSpec = new DependencyGraphSpec();
                dgSpec.AddRestore(restoreParameters.ProjectName);
                var projectSpec = new PackageSpec
                {
                    Name = restoreParameters.ProjectName,
                    FilePath = restoreParameters.ProjectName,
                    RestoreMetadata = CreateRestoreMetadata(restoreParameters),
                    TargetFrameworks = { CreateTargetFramework(restoreParameters) }
                };
                dgSpec.AddProject(projectSpec);

                providers.Add(new DependencyGraphSpecRequestProvider(providerCache, dgSpec));

                var restoreContext = new RestoreArgs
                {
                    CacheContext = cacheContext,
                    LockFileVersion = LockFileFormat.Version,
                    DisableParallel = false,
                    Log = logger,
                    MachineWideSettings = new XPlatMachineWideSetting(),
                    PreLoadedRequestProviders = providers,
                    AllowNoOp = true,
                    HideWarningsAndErrors = true
                };

                var restoreSummaries = await RestoreRunner.RunAsync(restoreContext, cancellationToken).ConfigureAwait(false);

                var result = new RestoreResult(
                    success: restoreSummaries.All(x => x.Success),
                    noOp: restoreSummaries.All(x => x.NoOpRestore),
                    errors: restoreSummaries.SelectMany(x => x.Errors).Select(x => x.Message).ToImmutableArray());

                return result;
            }
        }

        private static ProjectRestoreMetadata CreateRestoreMetadata(RestoreParams restoreParameters)
        {
            var metadata = new ProjectRestoreMetadata
            {
                ProjectUniqueName = restoreParameters.ProjectName,
                ProjectName = restoreParameters.ProjectName,
                ProjectStyle = ProjectStyle.PackageReference,
                ProjectPath = restoreParameters.ProjectName,
                OutputPath = restoreParameters.OutputPath,
                PackagesPath = restoreParameters.PackagesPath,
                ValidateRuntimeAssets = false,
                OriginalTargetFrameworks = { restoreParameters.TargetFramework.GetShortFolderName() }
            };

            foreach (var configPath in restoreParameters.ConfigFilePaths)
            {
                metadata.ConfigFilePaths.Add(configPath);
            }

            foreach (var source in restoreParameters.Sources)
            {
                metadata.Sources.Add(source);
            }

            return metadata;
        }

        private static TargetFrameworkInformation CreateTargetFramework(RestoreParams restoreParameters)
        {
            var targetFramework = new TargetFrameworkInformation
            {
                FrameworkName = restoreParameters.TargetFramework
            };

            if (restoreParameters.TargetFramework.Framework == ".NETCoreApp")
            {
                targetFramework.Dependencies.Add(new LibraryDependency(
                    libraryRange: new LibraryRange("Microsoft.NETCore.App", new VersionRange(new NuGetVersion(restoreParameters.TargetFramework.Version)), LibraryDependencyTarget.Package),
                    type: LibraryDependencyType.Platform,
                    includeType: LibraryIncludeFlags.All,
                    suppressParent: LibraryIncludeFlags.All,
                    noWarn: Array.Empty<NuGetLogCode>(),
                    autoReferenced: true));
            }

            if (restoreParameters.Packages != null)
            {
                foreach (var package in restoreParameters.Packages)
                {
                    targetFramework.Dependencies.Add(new LibraryDependency
                    {
                        LibraryRange = new LibraryRange(package.Id, package.VersionRange, LibraryDependencyTarget.Package)
                    });
                }
            }

            return targetFramework;
        }

        async Task<IReadOnlyList<INuGetPackage>> INuGetCompletionProvider.SearchPackagesAsync(string searchString, bool exactMatch, CancellationToken cancellationToken)
        {
            var packages = await GetPackagesAsync(searchString, includePrerelease: true, exactMatch, cancellationToken);
            return packages;
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

        #endregion
    }

    public class NuGetRestoreResult
    {
        public NuGetRestoreResult(IList<string> compileReferences, IList<string> runtimeReferences)
        {
            CompileReferences = compileReferences;
            RuntimeReferences = runtimeReferences;
        }

        public IList<string> CompileReferences { get; }
        public IList<string> RuntimeReferences { get; }
    }

    [Export]
    public sealed class NuGetDocumentViewModel : NotificationObject
    {
        private readonly NuGetViewModel _nuGetViewModel;
        private readonly ITelemetryProvider _telemetryProvider;
        private readonly SemaphoreSlim _restoreLock;
        private readonly HashSet<PackageRef> _referencedPackages;

        private bool _isRestoring;
        private CancellationTokenSource _restoreCts;
        private string _searchTerm;
        private bool _isSearching;
        private CancellationTokenSource _searchCts;
        private IReadOnlyList<PackageData> _packages;
        private bool _isPackagesMenuOpen;
        private bool _prerelease;
        private bool _restoreFailed;
        private NuGetFramework _targetFramework;
        private IReadOnlyList<string> _restoreErrors;

        [ImportingConstructor]
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public NuGetDocumentViewModel(NuGetViewModel nuGetViewModel, ICommandProvider commands, ITelemetryProvider telemetryProvider)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            _nuGetViewModel = nuGetViewModel;
            _telemetryProvider = telemetryProvider;
            _restoreLock = new SemaphoreSlim(1, 1);
            _referencedPackages = new HashSet<PackageRef>();
            _packages = Array.Empty<PackageData>();

            InstallPackageCommand = commands.Create<PackageData>(InstallPackage);
        }

        private void InstallPackage(PackageData package)
        {
            OnPackageInstalled(package);
        }

        public IDelegateCommand<PackageData> InstallPackageCommand { get; }

        private void OnPackageInstalled(PackageData package)
        {
            PackageInstalled?.Invoke(package);
        }

        public event Action<PackageData> PackageInstalled;

        public event Action<NuGetRestoreResult> RestoreCompleted;

        public bool IsSearching
        {
            get => _isSearching;
            private set => SetProperty(ref _isSearching, value);
        }

        public bool IsRestoring
        {
            get { return _isRestoring; }
            private set => SetProperty(ref _isRestoring, value);
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    PerformSearch();
                }
            }
        }

        public IReadOnlyList<PackageData> Packages
        {
            get => _packages;
            private set => SetProperty(ref _packages, value);
        }

        public void UpdatePackageReferences(IReadOnlyList<PackageRef> packages)
        {
            var changed = false;

            if (_referencedPackages.Count > 0 && (packages == null || packages.Count == 0))
            {
                _referencedPackages.Clear();

                changed = true;
            }
            else
            {
                if (_referencedPackages.RemoveWhere(p => !packages.Contains(p)) > 0)
                {
                    changed = true;
                }

                if (packages != null)
                {
                    foreach (var package in packages)
                    {
                        if (_referencedPackages.Add(package))
                        {
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                RefreshPackages();
            }
        }

        public string Id { get; set; }

        public string BuildPath { get; set; }

        public bool IsPackagesMenuOpen
        {
            get => _isPackagesMenuOpen;
            set => SetProperty(ref _isPackagesMenuOpen, value);
        }

        public bool ExactMatch { get; set; }

        public bool Prerelease
        {
            get => _prerelease;
            set
            {
                if (SetProperty(ref _prerelease, value))
                {
                    PerformSearch();
                }
            }
        }

        public bool RestoreFailed
        {
            get { return _restoreFailed; }
            private set { SetProperty(ref _restoreFailed, value); }
        }

        public IReadOnlyList<string> RestoreErrors
        {
            get { return _restoreErrors; }
            private set { SetProperty(ref _restoreErrors, value); }
        }

        public void SetTargetFramework(string targetFrameworkName)
        {
            _targetFramework = NuGetFramework.Parse(targetFrameworkName);
            RefreshPackages();
        }

        private void PerformSearch()
        {
            _searchCts?.Cancel();
            var searchCts = new CancellationTokenSource();
            var cancellationToken = searchCts.Token;
            _searchCts = searchCts;

            Task.Run(() => PerformSearch(SearchTerm, cancellationToken), cancellationToken);
        }

        private async Task PerformSearch(string searchTerm, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Packages = Array.Empty<PackageData>();
                IsPackagesMenuOpen = false;
                return;
            }

            IsSearching = true;
            try
            {
                try
                {
                    var packages = await Task.Run(() =>
                            _nuGetViewModel.GetPackagesAsync(searchTerm, includePrerelease: Prerelease,
                                exactMatch: ExactMatch, cancellationToken: cancellationToken), cancellationToken)
                        .ConfigureAwait(true);

                    Packages = packages;
                    IsPackagesMenuOpen = Packages.Count > 0;
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    _telemetryProvider.ReportError(e);
                }
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void RefreshPackages()
        {
            if (BuildPath == null || _targetFramework == null) return;

            _restoreCts?.Cancel();

            var packages = _referencedPackages?.ToArray();

            var restoreCts = new CancellationTokenSource();
            var cancellationToken = restoreCts.Token;
            _restoreCts = restoreCts;

            Task.Run(() => RefreshPackagesAsync(packages, cancellationToken), cancellationToken);
        }

        private async Task RefreshPackagesAsync(PackageRef[] packages, CancellationToken cancellationToken)
        {
            await _restoreLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            IsRestoring = true;
            try
            {
                var restoreParams = _nuGetViewModel.CreateRestoreParams();
                restoreParams.ProjectName = Id;
                restoreParams.OutputPath = BuildPath;
                restoreParams.Packages = packages;
                restoreParams.TargetFramework = _targetFramework;

                var lockFilePath = Path.Combine(BuildPath, "project.assets.json");
                IOUtilities.PerformIO(() => File.Delete(lockFilePath));

                var result = await NuGetViewModel.RestoreAsync(restoreParams, NullLogger.Instance, cancellationToken).ConfigureAwait(false);

                if (!result.Success)
                {
                    RestoreFailed = true;
                    RestoreErrors = result.Errors;
                    return;
                }

                RestoreFailed = false;
                RestoreErrors = Array.Empty<string>();

                if (result.NoOp)
                {
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                ParseLockFile(lockFilePath, cancellationToken);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                _telemetryProvider.ReportError(e);
            }
            finally
            {
                _restoreLock.Release();
                IsRestoring = false;
            }
        }

        private void ParseLockFile(string lockFilePath, CancellationToken cancellationToken)
        {
            List<string> compile, runtime;
            JObject obj;
            using (var reader = File.OpenText(lockFilePath))
            {
                obj = NuGetViewModel.LoadJson(reader);
            }

            (compile, runtime) = NuGetViewModel.ReadProjectLockJson(obj,
                                                                    _nuGetViewModel.GlobalPackageFolder,
                                                                    _targetFramework.DotNetFrameworkName);

            TransformLockFileToDepsFile(obj, _targetFramework.DotNetFrameworkName);

            cancellationToken.ThrowIfCancellationRequested();

            using (var writer = new JsonTextWriter(File.CreateText(lockFilePath)) { Formatting = Formatting.Indented })
            {
                obj.WriteTo(writer);
            }

            cancellationToken.ThrowIfCancellationRequested();

            RestoreCompleted?.Invoke(new NuGetRestoreResult(compile, runtime));
        }

        private static void TransformLockFileToDepsFile(JObject obj, string targetFramework)
        {
            foreach (var p in obj.Properties().Where(p => p.Name != "targets" && p.Name != "libraries").ToArray())
            {
                p.Remove();
            }

            obj.AddFirst(new JProperty("runtimeTarget", new JObject(new JProperty("name", targetFramework))));

            var libraries = (JObject)obj["libraries"];

            foreach (var fx in ((JObject)obj["targets"]).Properties())
            {
                foreach (var p in fx.Value.Children<JProperty>().Where(p => IsRuntimeEmptyOrPlaceholder(p)).ToArray())
                {
                    p.Remove();
                    libraries.Remove(p.Name);
                }

                ((JObject)fx.Value).Add(new JProperty("RoslynPad.Runtime/1.0.0", new JObject(
                    new JProperty("type", "project"),
                    new JProperty("runtime", new JObject(
                        new JProperty("RoslynPad.Runtime.dll", new JObject()))))));
            }

            foreach (var p in libraries.Properties())
            {
                p.Value["serviceable"] = true;
                ((JObject)p.Value).Remove("files");
            }

            libraries.Add(new JProperty("RoslynPad.Runtime/1.0.0", new JObject(
                new JProperty("type", "project"),
                new JProperty("serviceable", false),
                new JProperty("sha512", ""))));

            bool IsRuntimeEmptyOrPlaceholder(JProperty p)
            {
                var runtime = p.Value["runtime"] as JObject;
                return runtime == null ||
                       runtime.Properties().All(pp => NuGetViewModel.IsPlaceholder(pp.Name));
            }
        }
    }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public class RestoreParams
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
    {
        public string ProjectName { get; set; }
        public NuGetFramework TargetFramework { get; set; }
        public string OutputPath { get; set; }
        public string PackagesPath { get; set; }
        public IList<string> ConfigFilePaths { get; set; } = new List<string>();
        public IList<PackageSource> Sources { get; set; } = new List<PackageSource>();
        public IList<PackageRef> Packages { get; set; } = new List<PackageRef>();
    }

    public class PackageRef : IEquatable<PackageRef?>
    {
        public PackageRef(string id, VersionRange versionRange)
        {
            Id = id;
            VersionRange = versionRange;
        }

        public string Id { get; }
        public VersionRange VersionRange { get; }

        public bool Equals(PackageRef? other)
        {
            return other == null
                ? false
                : Id == other.Id && VersionRange.Equals(other.VersionRange);
        }

        public override bool Equals(object obj) => Equals(obj as PackageRef);

        public override int GetHashCode()
        {
            return (Id, VersionRange).GetHashCode();
        }
    }

    public class RestoreResult
    {
        public RestoreResult(IReadOnlyList<string> errors, bool success, bool noOp)
        {
            Errors = errors;
            Success = success;
            NoOp = noOp;
        }

        public IReadOnlyList<string> Errors { get; }
        public bool Success { get; }
        public bool NoOp { get; }
    }

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
            OtherVersions = versions.Select(x => new PackageData(Id, x.Version)).OrderByDescending(x => x.Version).ToImmutableArray();
        }
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