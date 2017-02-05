using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn.SymbolSearch
{
    [ExportWorkspaceService(typeof(Microsoft.CodeAnalysis.Packaging.IPackageInstallerService), ServiceLayer.Host), Shared]
    internal class DefaultPackageInstallerService : Microsoft.CodeAnalysis.Packaging.IPackageInstallerService
    {
        private readonly IPackageInstallerService _implementation;

        [ImportingConstructor]
        public DefaultPackageInstallerService(IPackageInstallerService implementation)
        {
            _implementation = implementation;
        }

        public bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName)
        {
            return _implementation.IsInstalled(workspace, projectId, packageName);
        }

        public bool TryInstallPackage(Workspace workspace, DocumentId documentId, string source, string packageName, string versionOpt,
            bool includePrerelease, CancellationToken cancellationToken)
        {
            return _implementation.TryInstallPackage(workspace, documentId, source, packageName, versionOpt, includePrerelease, cancellationToken);
        }

        public ImmutableArray<string> GetInstalledVersions(string packageName)
        {
            return _implementation.GetInstalledVersions(packageName);
        }

        public IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version)
        {
            return _implementation.GetProjectsWithInstalledPackage(solution, packageName, version);
        }

        public void ShowManagePackagesDialog(string packageName)
        {
            _implementation.ShowManagePackagesDialog(packageName);
        }

        public bool IsEnabled => _implementation.IsEnabled;

        public ImmutableArray<Microsoft.CodeAnalysis.Packaging.PackageSource> PackageSources => 
            _implementation.PackageSources.SelectAsArray(x => new Microsoft.CodeAnalysis.Packaging.PackageSource(x.Name, x.Source));

        public event EventHandler PackageSourcesChanged
        {
            add { _implementation.PackageSourcesChanged += value; }
            remove { _implementation.PackageSourcesChanged -= value; }
        }
    }

    public interface IPackageInstallerService
    {
        bool IsEnabled { get; }

        bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName);

        bool TryInstallPackage(Workspace workspace, DocumentId documentId,
            string source, string packageName,
            string versionOpt, bool includePrerelease,
            CancellationToken cancellationToken);

        ImmutableArray<string> GetInstalledVersions(string packageName);

        IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version);

        void ShowManagePackagesDialog(string packageName);

        ImmutableArray<PackageSource> PackageSources { get; }

        event EventHandler PackageSourcesChanged;
    }

    public struct PackageSource : IEquatable<PackageSource>
    {
        public readonly string Name;
        public readonly string Source;

        public PackageSource(string name, string source)
        {
            Name = name;
            Source = source;
        }

        public override bool Equals(object obj)
            => obj is PackageSource && Equals((PackageSource)obj);

        public bool Equals(PackageSource other)
            => Name == other.Name && Source == other.Source;

        public override int GetHashCode()
            => Hash.Combine(Name, Source.GetHashCode());
    }
}