using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn.SymbolSearch;

namespace RoslynPad.UI
{
    [Export(typeof(IPackageInstallerService)), Shared]
    internal class PackageInstallerService : IPackageInstallerService
    {
        public bool IsEnabled => true;

        public bool IsInstalled(Workspace workspace, ProjectId projectId, string packageName)
        {
            return false;
        }

        public bool TryInstallPackage(Workspace workspace, DocumentId documentId, string source, string packageName, string versionOpt,
            bool includePrerelease, CancellationToken cancellationToken)
        {
            return true;
        }

        public ImmutableArray<string> GetInstalledVersions(string packageName)
        {
            return ImmutableArray<string>.Empty;
        }

        public IEnumerable<Project> GetProjectsWithInstalledPackage(Solution solution, string packageName, string version)
        {
            return ImmutableArray<Project>.Empty;
        }

        public void ShowManagePackagesDialog(string packageName)
        {
        }

        public ImmutableArray<PackageSource> PackageSources => 
            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            ImmutableArray<PackageSource>.Empty.Add(new PackageSource("P", "S"));

        public event EventHandler PackageSourcesChanged
        {
            add { }
            remove { }
        }
    }
}