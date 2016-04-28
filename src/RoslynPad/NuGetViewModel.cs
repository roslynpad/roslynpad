using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v2;
using NuGet.Resolver;
using NuGet.Versioning;
using RoslynPad.Utilities;
using IPackageSourceProvider = NuGet.Configuration.IPackageSourceProvider;
using ISettings = NuGet.Configuration.ISettings;
using PackageReference = NuGet.Packaging.PackageReference;
using PackageSource = NuGet.Configuration.PackageSource;
using PackageSourceProvider = NuGet.Configuration.PackageSourceProvider;
using Settings = NuGet.Configuration.Settings;
using System.Reflection;
using NuGet.Protocol.Core.v3;
using IMachineWideSettings = NuGet.Configuration.IMachineWideSettings;

namespace RoslynPad
{
    internal sealed class NuGetViewModel : NotificationObject
    {
        private const string TargetFrameworkName = "net46";
        private const string TargetFrameworkFullName = ".NET Framework, Version=4.6";

        private readonly ISettings _settings;
        private readonly PackageSourceProvider _sourceProvider;
        private readonly IEnumerable<PackageSource> _packageSources;
        private readonly CommandLineSourceRepositoryProvider _sourceRepositoryProvider;
        private readonly Lazy<Task> _initializationTask;

        private string _searchTerm;
        private IReadOnlyList<IPackage> _packages;
        private CancellationTokenSource _cts;
        private bool _hasPackages;
        private AggregateRepository _repository;
        private bool _isBusy;
        private bool _isEnabled;

        public string GlobalPackageFolder { get; }

        public NuGetViewModel()
        {
            _settings = Settings.LoadDefaultSettings(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                configFileName: null,
                machineWideSettings: new CommandLineMachineWideSettings());

            _sourceProvider = new PackageSourceProvider(_settings);

            GlobalPackageFolder = SettingsUtility.GetGlobalPackagesFolder(_settings);

            _packageSources = GetPackageSources();

            _sourceRepositoryProvider = new CommandLineSourceRepositoryProvider(_sourceProvider);

            _initializationTask = new Lazy<Task>(Initialize);

            InstallPackageCommand = new DelegateCommand<IPackage>(InstallPackage);

            IsEnabled = true;
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set { SetProperty(ref _isBusy, value); }
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            private set { SetProperty(ref _isEnabled, value); }
        }

        public DelegateCommand<IPackage> InstallPackageCommand { get; }

        public event Action<IPackage, NuGetInstallResult> PackageInstalled;

        private void OnPackageInstalled(IPackage package, NuGetInstallResult result)
        {
            PackageInstalled?.Invoke(package, result);
        }

        public string SearchTerm
        {
            get { return _searchTerm; }
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();
                    PerformSearch(value, _cts.Token);
                }
            }
        }

        public IReadOnlyList<IPackage> Packages
        {
            get { return _packages; }
            private set { SetProperty(ref _packages, value); }
        }

        public bool HasPackages
        {
            get { return _hasPackages; }
            set { SetProperty(ref _hasPackages, value); }
        }

        public bool ExactMatch { get; set; }

        private async void PerformSearch(string searchTerm, CancellationToken cancellationToken)
        {
            IsBusy = true;
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    HasPackages = false;
                    Packages = null;
                    return;
                }
                try
                {
                    var packages = await Task.Run(() => GetPackagesAsync(searchTerm, true, false), cancellationToken).ConfigureAwait(false);
                    var list = new List<IPackage>();
                    foreach (var package in packages)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        list.Add(package);
                    }
                    HasPackages = list.Count > 0;
                    Packages = list.AsReadOnly();
                }
                catch (OperationCanceledException)
                {
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<IEnumerable<IPackage>> GetPackagesAsync(string searchTerm, bool includePrerelease, bool allVersions)
        {
            await _initializationTask.Value.ConfigureAwait(false);
            return GetPackages(searchTerm, includePrerelease, allVersions);
        }

        public async Task InstallPackage(IPackage package)
        {
            IsBusy = true;
            IsEnabled = false;
            try
            {
                var result = await InstallPackage(package.Id, new NuGetVersion(package.Version.ToNormalizedString()), prerelease: true).ConfigureAwait(false);

                OnPackageInstalled(package, result);
            }
            finally
            {
                IsBusy = false;
                IsEnabled = true;
            }
        }

        private async Task<NuGetInstallResult> InstallPackage(
            string packageId,
            NuGetVersion version,
            bool prerelease)
        {
            var installPath = Path.Combine(Path.GetTempPath(), "testnuget");

            var projectContext = new EmptyNuGetProjectContext
            {
                PackageExtractionContext = new PackageExtractionContext()
            };

            var references = new List<string>();
            var frameworkReferences = new List<string>();
            var projectSystem = new DelegateNuGetProjectSystem(projectContext, (reference, isFrameworkReference) =>
            {
                if (isFrameworkReference) frameworkReferences.Add(reference);
                else references.Add(reference);
            });

            var project = new MSBuildNuGetProject(projectSystem, installPath, installPath);
            OverrideProject(project);

            var packageManager = new NuGetPackageManager(_sourceRepositoryProvider, _settings, installPath);

            var primaryRepositories = _packageSources.Select(_sourceRepositoryProvider.CreateRepository).ToArray();

            var resolutionContext = new ResolutionContext(
                DependencyBehavior.Lowest,
                includePrelease: prerelease,
                includeUnlisted: true,
                versionConstraints: VersionConstraints.None);

            if (version == null)
            {
                // Find the latest version using NuGetPackageManager
                version = await NuGetPackageManager.GetLatestVersionAsync(
                    packageId,
                    project,
                    resolutionContext,
                    primaryRepositories,
                    CancellationToken.None).ConfigureAwait(false);

                if (version == null)
                {
                    throw new Exception("Unable to find package");
                }
            }

            var packageIdentity = new PackageIdentity(packageId, version);

            await packageManager.InstallPackageAsync(
                project,
                packageIdentity,
                resolutionContext,
                projectContext,
                primaryRepositories,
                Enumerable.Empty<SourceRepository>(),
                CancellationToken.None).ConfigureAwait(false);

            return new NuGetInstallResult(references.AsReadOnly(), frameworkReferences.AsReadOnly());
        }

        private static void OverrideProject(MSBuildNuGetProject project)
        {
            var folderNuGetProjectField = typeof(MSBuildNuGetProject).GetTypeInfo()
                .DeclaredFields.First(x => x.FieldType == typeof(FolderNuGetProject));
            folderNuGetProjectField.SetValue(project, new DummyFolderNuGetProject());

            var packagesConfigNuGetProjectField = typeof(MSBuildNuGetProject).GetTypeInfo()
                .DeclaredFields.First(x => x.FieldType == typeof(PackagesConfigNuGetProject));
            packagesConfigNuGetProjectField.SetValue(project, new DummyPackagesConfigNuGetProject(project.Metadata));
        }

        private async Task Initialize()
        {
            var listEndpoints = await GetListEndpointsAsync(_sourceProvider).ConfigureAwait(false);

            var repositoryFactory = new PackageRepositoryFactory();

            var repositories = listEndpoints
                .Select(s => repositoryFactory.CreateRepository(s))
                .ToList();

            _repository = new AggregateRepository(repositories);
        }

        private static async Task<IList<string>> GetListEndpointsAsync(IPackageSourceProvider sourceProvider)
        {
            var configurationSources = sourceProvider.LoadPackageSources()
                .Where(p => p.IsEnabled)
                .ToList();

            var sourceRepositoryProvider = new CommandLineSourceRepositoryProvider(sourceProvider);
            var listCommandResourceTasks = configurationSources.Select(source => sourceRepositoryProvider.CreateRepository(source)).Select(sourceRepository => sourceRepository.GetResourceAsync<ListCommandResource>()).ToList();
            var listCommandResources = await Task.WhenAll(listCommandResourceTasks).ConfigureAwait(false);

            var listEndpoints = new List<string>();
            foreach (var listCommandResource in listCommandResources)
            {
                string listEndpoint = null;
                if (listCommandResource != null)
                {
                    listEndpoint = listCommandResource.GetListEndpoint();
                }
                if (listEndpoint != null)
                {
                    listEndpoints.Add(listEndpoint);
                }
            }

            return listEndpoints;
        }

        private IEnumerable<IPackage> GetPackages(string searchTerm, bool includePrerelease, bool allVersions)
        {
            if (ExactMatch)
            {
                var exactResult = _repository.FindPackagesById(searchTerm);
                if (!allVersions)
                {
                    exactResult = includePrerelease
                        ? exactResult.Where(x => x.IsAbsoluteLatestVersion)
                        : exactResult.Where(p => p.IsLatestVersion);
                }
                return exactResult;
            }

            IQueryable<IPackage> packages = _repository.Search(
                searchTerm,
                targetFrameworks: new[] { TargetFrameworkFullName },
                allowPrereleaseVersions: includePrerelease);

            if (allVersions)
            {
                return packages.OrderBy(p => p.Id);
            }
            packages = includePrerelease ? packages.Where(p => p.IsAbsoluteLatestVersion) : packages.Where(p => p.IsLatestVersion);

            var result = packages.AsEnumerable();
            result = result.Where(PackageExtensions.IsListed);
            return result.Where(p => includePrerelease || p.IsReleaseVersion())
                .AsCollapsed()
                .OrderBy(x => x.Id);
        }

        private IEnumerable<PackageSource> GetPackageSources()
        {
            var availableSources = _sourceProvider.LoadPackageSources().Where(source => source.IsEnabled);
            var packageSources = new List<PackageSource>();
            
            if (!string.IsNullOrEmpty(GlobalPackageFolder) && Directory.Exists(GlobalPackageFolder))
            {
                packageSources.Add(new V2PackageSource(GlobalPackageFolder,
                    () => new LocalPackageRepository(GlobalPackageFolder)));
            }

            packageSources.AddRange(availableSources);

            return packageSources;
        }

        #region Inner Classes

        private class DummyPackagePathResolver : PackagePathResolver
        {
            public DummyPackagePathResolver() : base(Directory.GetCurrentDirectory(), true)
            {
            }

            public override string GetInstalledPackageFilePath(PackageIdentity packageIdentity)
            {
                return $"{packageIdentity.Id}\\{packageIdentity.Version.ToNormalizedString()}\\";
            }
        }

        private class DummyFolderNuGetProject : FolderNuGetProject
        {
            public DummyFolderNuGetProject() : base(Directory.GetCurrentDirectory(), new DummyPackagePathResolver())
            {
            }

            public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync(CancellationToken token)
            {
                return Task.FromResult(Enumerable.Empty<PackageReference>());
            }

            public override Task<bool> InstallPackageAsync(PackageIdentity packageIdentity, DownloadResourceResult downloadResourceResult,
                INuGetProjectContext nuGetProjectContext, CancellationToken token)
            {
                return Task.FromResult(true);
            }

            public override Task PostProcessAsync(INuGetProjectContext nuGetProjectContext, CancellationToken token)
            {
                return Task.CompletedTask;
            }

            public override Task PreProcessAsync(INuGetProjectContext nuGetProjectContext, CancellationToken token)
            {
                return Task.CompletedTask;
            }

            public override Task<bool> UninstallPackageAsync(PackageIdentity packageIdentity, INuGetProjectContext nuGetProjectContext,
                CancellationToken token)
            {
                return Task.FromResult(true);
            }
        }

        private class DummyPackagesConfigNuGetProject : PackagesConfigNuGetProject
        {
            public DummyPackagesConfigNuGetProject(IReadOnlyDictionary<string, object> metadata) : base(Directory.GetCurrentDirectory(), metadata.ToDictionary(x => x.Key, x => x.Value))
            {
            }

            public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync(CancellationToken token)
            {
                return Task.FromResult(Enumerable.Empty<PackageReference>());
            }

            public override Task<bool> InstallPackageAsync(PackageIdentity packageIdentity, DownloadResourceResult downloadResourceResult,
                INuGetProjectContext nuGetProjectContext, CancellationToken token)
            {
                return Task.FromResult(true);
            }

            public override Task PostProcessAsync(INuGetProjectContext nuGetProjectContext, CancellationToken token)
            {
                return Task.CompletedTask;
            }

            public override Task PreProcessAsync(INuGetProjectContext nuGetProjectContext, CancellationToken token)
            {
                return Task.CompletedTask;
            }

            public override Task<bool> UninstallPackageAsync(PackageIdentity packageIdentity, INuGetProjectContext nuGetProjectContext,
                CancellationToken token)
            {
                return Task.FromResult(true);
            }
        }

        private class DelegateNuGetProjectSystem : IMSBuildNuGetProjectSystem
        {
            private readonly Action<string, bool> _addReference;

            public DelegateNuGetProjectSystem(INuGetProjectContext projectContext,
                Action<string, bool> addReference)
            {
                NuGetProjectContext = projectContext;
                _addReference = addReference;
            }

            public NuGetFramework TargetFramework { get; } = NuGetFramework.Parse(TargetFrameworkName);

            public void AddReference(string referencePath)
            {
                _addReference(referencePath, false);
            }

            public void AddFrameworkReference(string name)
            {
                _addReference(name, true);
            }

            #region Not used

            public void SetNuGetProjectContext(INuGetProjectContext nuGetProjectContext)
            {
            }

            public void AddFile(string path, Stream stream)
            {
            }

            public void AddExistingFile(string path)
            {
            }

            public void RemoveFile(string path)
            {
            }

            public bool FileExistsInProject(string path)
            {
                return false;
            }

            public void RemoveReference(string name)
            {
            }

            public bool ReferenceExists(string name)
            {
                return false;
            }

            public void AddImport(string targetFullPath, ImportLocation location)
            {
            }

            public void RemoveImport(string targetFullPath)
            {
            }

            public dynamic GetPropertyValue(string propertyName)
            {
                return null;
            }

            public string ResolvePath(string path)
            {
                return null;
            }

            public bool IsSupportedFile(string path)
            {
                return true;
            }

            public void AddBindingRedirects()
            {
            }

            public Task ExecuteScriptAsync(PackageIdentity identity, string packageInstallPath, string scriptRelativePath,
                NuGetProject nuGetProject, bool throwOnFailure)
            {
                return Task.CompletedTask;
            }

            public void BeginProcessing()
            {
            }

            public void RegisterProcessedFiles(IEnumerable<string> files)
            {
            }

            public void EndProcessing()
            {
            }

            public void DeleteDirectory(string path, bool recursive)
            {
            }

            public IEnumerable<string> GetFiles(string path, string filter, bool recursive)
            {
                return Enumerable.Empty<string>();
            }

            public IEnumerable<string> GetFullPaths(string fileName)
            {
                return Enumerable.Empty<string>();
            }

            public IEnumerable<string> GetDirectories(string path)
            {
                return Enumerable.Empty<string>();
            }

            public string ProjectName => "P";
            public string ProjectUniqueName => "P";
            public string ProjectFullPath => "P";

            public INuGetProjectContext NuGetProjectContext { get; }

            #endregion
        }

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
                _resourceProviders.AddRange(Repository.Provider.GetCoreV2());
                _resourceProviders.AddRange(Repository.Provider.GetCoreV3());

                // Create repositories
                _repositories = PackageSourceProvider.LoadPackageSources()
                    .Where(s => s.IsEnabled)
                    .Select(CreateRepository)
                    .ToList();
            }

            /// <summary>
            /// Retrieve repositories that have been cached.
            /// </summary>
            public IEnumerable<SourceRepository> GetRepositories()
            {
                return _repositories;
            }

            /// <summary>
            /// Create a repository and add it to the cache.
            /// </summary>
            public SourceRepository CreateRepository(PackageSource source)
            {
                return _cachedSources.GetOrAdd(source, new SourceRepository(source, _resourceProviders));
            }

            public IPackageSourceProvider PackageSourceProvider { get; }
        }

        private class CommandLineMachineWideSettings : IMachineWideSettings
        {
            private readonly Lazy<IEnumerable<Settings>> _settings;

            public CommandLineMachineWideSettings()
            {
                var baseDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "nuget",
                        "Config");
                _settings = new Lazy<IEnumerable<Settings>>(
                    () => NuGet.Configuration.Settings.LoadMachineWideSettings(baseDirectory));
            }

            public IEnumerable<Settings> Settings => _settings.Value;
        }

        #endregion
    }

    internal sealed class NuGetInstallResult
    {
        public NuGetInstallResult(IReadOnlyList<string> references, IReadOnlyList<string> frameworkReferences)
        {
            References = references;
            FrameworkReferences = frameworkReferences;
        }

        public IReadOnlyList<string> References { get; }
        public IReadOnlyList<string> FrameworkReferences { get; }
    }
}