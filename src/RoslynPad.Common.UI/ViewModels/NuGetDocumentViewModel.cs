using System.Composition;
using RoslynPad.Utilities;

namespace RoslynPad.UI;

[Export]
public sealed class NuGetDocumentViewModel : NotificationObject
{
    private readonly NuGetViewModel _nuGetViewModel;
    private readonly ITelemetryProvider _telemetryProvider;

    private string? _searchTerm;
    private bool _isSearching;
    private CancellationTokenSource? _searchCts;
    private bool _isPackagesMenuOpen;
    private bool _prerelease;
    private IReadOnlyList<PackageData> _packages;

    public IReadOnlyList<PackageData> Packages
    {
        get => _packages;
        private set => SetProperty(ref _packages, value);
    }

    [ImportingConstructor]
    public NuGetDocumentViewModel(NuGetViewModel nuGetViewModel, ICommandProvider commands, ITelemetryProvider telemetryProvider)
    {
        _nuGetViewModel = nuGetViewModel;
        _telemetryProvider = telemetryProvider;
        _packages = [];

        InstallPackageCommand = commands.Create<PackageData>(InstallPackage);
    }

    private void InstallPackage(PackageData? package)
    {
        if (package == null)
        {
            return;
        }

        OnPackageInstalled(package);
    }

    public IDelegateCommand<PackageData> InstallPackageCommand { get; }

    private void OnPackageInstalled(PackageData package)
    {
        PackageInstalled?.Invoke(package);
    }

    public event Action<PackageData>? PackageInstalled;

    public bool IsSearching
    {
        get => _isSearching;
        private set => SetProperty(ref _isSearching, value);
    }

    public string? SearchTerm
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

    private void PerformSearch()
    {
        if (string.IsNullOrEmpty(SearchTerm))
        {
            return;
        }

        _searchCts?.Cancel();
        var searchCts = new CancellationTokenSource();
        var cancellationToken = searchCts.Token;
        _searchCts = searchCts;

        _ = Task.Run(() => PerformSearch(SearchTerm, cancellationToken), cancellationToken);
    }

    private async Task PerformSearch(string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Packages = [];
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

                cancellationToken.ThrowIfCancellationRequested();

                foreach (var package in packages)
                {
                    package.InstallPackageCommand = InstallPackageCommand;
                }

                Packages = packages;
                IsPackagesMenuOpen = Packages.Count > 0;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _telemetryProvider.ReportError(e);
            }
        }
        finally
        {
            IsSearching = false;
        }
    }
}
