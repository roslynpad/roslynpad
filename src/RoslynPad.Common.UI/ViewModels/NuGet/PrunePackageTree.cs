// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace RoslynPad.UI.NuGet
{
    /// <summary>
    /// Helpers to reduce down the gathered package dependency info to the allowed set
    /// </summary>
    public static class PrunePackageTree
    {
        /// <summary>
        /// Remove some of the prerelease packages in update scenarios
        /// </summary>
        public static IEnumerable<SourcePackageDependencyInfo> PrunePrereleaseExceptAllowed(IEnumerable<SourcePackageDependencyInfo> packages, IEnumerable<PackageIdentity> installedPackages, bool isUpdateAll)
        {
            var allowedPackageIdentity = new HashSet<PackageIdentity>(installedPackages.Where(p => p.HasVersion && p.Version.IsPrerelease), PackageIdentityComparer.Default);

            if (isUpdateAll)
            {
                // If this is an Update All scenario then we will allow package that are already prerelease to pick any other prerelease alternatievs

                var allowedPackageId = new HashSet<string>(allowedPackageIdentity.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);

                return packages.Where(p => !(p.HasVersion && p.Version.IsPrerelease) || allowedPackageId.Contains(p.Id));
            }
            else
            {
                // Else a specific package is being updated and we will simply allow existing packages to remain as they are

                return packages.Where(p => !(p.HasVersion && p.Version.IsPrerelease) || allowedPackageIdentity.Contains(p));
            }
        }

        /// <summary>
        /// Remove all prerelease packages for stable targets
        /// </summary>
        public static IEnumerable<SourcePackageDependencyInfo> PrunePreleaseForStableTargets(IEnumerable<SourcePackageDependencyInfo> packages, IEnumerable<PackageIdentity> targets, IEnumerable<PackageIdentity> packagesToInstall)
        {
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            CollectAllowedFromTargets(targets, allowed);
            CollectAllowedFromDependenciesOfPackagesToInstall(packages, packagesToInstall, allowed);

            return packages.Where(p => !(p.HasVersion && p.Version.IsPrerelease) || allowed.Contains(p.Id));
        }

        private static void CollectAllowedFromTargets(IEnumerable<PackageIdentity> targets, HashSet<string> allowed)
        {
            foreach (var p in targets.Where(p => p.HasVersion && p.Version.IsPrerelease))
            {
                allowed.Add(p.Id);
            }
        }

        private static void CollectAllowedFromDependenciesOfPackagesToInstall(IEnumerable<SourcePackageDependencyInfo> packages, IEnumerable<PackageIdentity> packagesToInstall, HashSet<string> allowed)
        {
            var prereleasePackageToInstall = new HashSet<PackageIdentity>(packagesToInstall.Where(p => p.HasVersion && p.Version.IsPrerelease), PackageIdentityComparer.Default);

            var visitedNodes = new HashSet<SourcePackageDependencyInfo>();
            var packagesDict = packages.ToLookup(p => p.Id).ToDictionary(p => p.Key, p => p.ToArray());

            foreach (var packageToInstall in packages.Where(p => prereleasePackageToInstall.Contains(p)))
            {
                WalkDependencies(packagesDict, packageToInstall, allowed, visitedNodes);
            }
        }

        private static void WalkDependencies(IDictionary<string, SourcePackageDependencyInfo[]> packagesDict, SourcePackageDependencyInfo packageToInstall, HashSet<string> allowed, HashSet<SourcePackageDependencyInfo> visitedNodes)
        {
            var queue = new Queue<SourcePackageDependencyInfo>();
            queue.Enqueue(packageToInstall);

            while (queue.Count > 0)
            {
                var package = queue.Dequeue();
                foreach (var dependency in package.Dependencies)
                {
                    allowed.Add(dependency.Id);
                    SourcePackageDependencyInfo[] packages;
                    if (packagesDict.TryGetValue(dependency.Id, out packages))
                    {
                        foreach (var dependentPackage in packages)
                        {
                            if (visitedNodes.Add(dependentPackage))
                            {
                                queue.Enqueue(dependentPackage);
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<SourcePackageDependencyInfo> PruneDowngrades(IEnumerable<SourcePackageDependencyInfo> packages, IEnumerable<global::NuGet.Packaging.PackageReference> packageReferences)
        {
            // prune every package that is less that the currently installed package

            IDictionary<string, NuGetVersion> installed = new Dictionary<string, NuGetVersion>(StringComparer.OrdinalIgnoreCase);
            foreach (var packageReference in packageReferences)
            {
                installed.Add(packageReference.PackageIdentity.Id, packageReference.PackageIdentity.Version);
            }

            return packages.Where(package =>
                (package.HasVersion && installed.ContainsKey(package.Id))
                    ?
                    installed[package.Id] <= package.Version : true);
        }

        public static IEnumerable<SourcePackageDependencyInfo> PruneDisallowedVersions(IEnumerable<SourcePackageDependencyInfo> packages, IEnumerable<global::NuGet.Packaging.PackageReference> packageReferences)
        {
            var result = packages;
            foreach (var packageReference in packageReferences)
            {
                result = RemoveDisallowedVersions(result, packageReference);
            }

            return result;
        }

        /// <summary>
        /// Remove all versions of a package id from the list, except for the target version
        /// </summary>
        public static IEnumerable<SourcePackageDependencyInfo> RemoveAllVersionsForIdExcept(IEnumerable<SourcePackageDependencyInfo> packages, PackageIdentity target)
        {
            var comparer = VersionComparer.VersionRelease;

            return packages.Where(p => !StringComparer.OrdinalIgnoreCase.Equals(target.Id, p.Id) ||
                                       (StringComparer.OrdinalIgnoreCase.Equals(target.Id, p.Id) && comparer.Equals(p.Version, target.Version)));
        }

        /// <summary>
        /// Keep only stable versions of a package
        /// </summary>
        public static IEnumerable<SourcePackageDependencyInfo> RemoveAllPrereleaseVersionsForId(IEnumerable<SourcePackageDependencyInfo> packages, string id)
        {
            return packages.Where(p => !StringComparer.OrdinalIgnoreCase.Equals(id, p.Id) ||
                                       (StringComparer.OrdinalIgnoreCase.Equals(id, p.Id) && !p.Version.IsPrerelease));
        }

        /// <summary>
        /// Clear out all versions less than the minimuim. This can be used to prevent downgrading
        /// </summary>
        public static IEnumerable<SourcePackageDependencyInfo> RemoveAllVersionsLessThan(IEnumerable<SourcePackageDependencyInfo> packages, PackageIdentity minimum)
        {
            var comparer = VersionComparer.VersionRelease;

            return packages.Where(p => !StringComparer.OrdinalIgnoreCase.Equals(minimum.Id, p.Id) ||
                                       (StringComparer.OrdinalIgnoreCase.Equals(minimum.Id, p.Id) && comparer.Compare(p.Version, minimum.Version) >= 0));
        }

        // TODO: Consider removing elements from the collection and check if that is better in performance
        public static IEnumerable<SourcePackageDependencyInfo> RemoveDisallowedVersions(IEnumerable<SourcePackageDependencyInfo> packages, global::NuGet.Packaging.PackageReference packageReference)
        {
            if (packageReference.AllowedVersions != null)
            {
                return packages.Where(p => !StringComparer.OrdinalIgnoreCase.Equals(p.Id, packageReference.PackageIdentity.Id)
                                           || packageReference.AllowedVersions.Satisfies(p.Version));
            }
            return packages;
        }

        /// <summary>
        /// This is used in update scenarios ro remove packages that are of the same Id but different version than the primartTargets 
        /// </summary>
        public static IEnumerable<SourcePackageDependencyInfo> PruneByPrimaryTargets(IEnumerable<SourcePackageDependencyInfo> packages, IEnumerable<PackageIdentity> primaryTargets)
        {
            var targets = new Dictionary<string, NuGetVersion>(StringComparer.OrdinalIgnoreCase);
            foreach (var primaryTarget in primaryTargets)
            {
                targets.Add(primaryTarget.Id, primaryTarget.Version);
            }

            return packages.Where(p => !targets.ContainsKey(p.Id) || (targets.ContainsKey(p.Id) && targets[p.Id] == p.Version));
        }

        public static IEnumerable<SourcePackageDependencyInfo> PruneAllButHighest(IEnumerable<SourcePackageDependencyInfo> packages, string packageId)
        {
            SourcePackageDependencyInfo highest = null;
            foreach (var package in packages)
            {
                if (string.Equals(package.Id, packageId, StringComparison.OrdinalIgnoreCase))
                {
                    if (highest == null || highest.Version < package.Version)
                    {
                        highest = package;
                    }
                }
            }

            if (highest == null)
            {
                return packages;
            }
            else
            {
                return packages.Where(p => !p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase) || p == highest);
            }
        }

        public static IEnumerable<SourcePackageDependencyInfo> PruneByUpdateConstraints(IEnumerable<SourcePackageDependencyInfo> packages, IEnumerable<global::NuGet.Packaging.PackageReference> packageReferences, VersionConstraints versionConstraints)
        {
            var installed = new Dictionary<string, NuGetVersion>(StringComparer.OrdinalIgnoreCase);
            foreach (var packageReference in packageReferences)
            {
                installed[packageReference.PackageIdentity.Id] = packageReference.PackageIdentity.Version;
            }

            return packages.Where(p => !installed.ContainsKey(p.Id) || MeetsVersionConstraints(p.Version, installed[p.Id], versionConstraints));
        }

        private static bool MeetsVersionConstraints(NuGetVersion newVersion, NuGetVersion existingVersion, VersionConstraints versionConstraints)
        {
            return
                (!versionConstraints.HasFlag(VersionConstraints.ExactMajor) || newVersion.Major == existingVersion.Major)
                &&
                (!versionConstraints.HasFlag(VersionConstraints.ExactMinor) || newVersion.Minor == existingVersion.Minor)
                &&
                (!versionConstraints.HasFlag(VersionConstraints.ExactPatch) || newVersion.Patch == existingVersion.Patch)
                &&
                (!versionConstraints.HasFlag(VersionConstraints.ExactRelease) || newVersion.Release.Equals(existingVersion.Release, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsExactVersion(VersionConstraints versionConstraints)
        {
            return
                versionConstraints.HasFlag(VersionConstraints.ExactMajor)
                &&
                versionConstraints.HasFlag(VersionConstraints.ExactMinor)
                &&
                versionConstraints.HasFlag(VersionConstraints.ExactPatch)
                &&
                versionConstraints.HasFlag(VersionConstraints.ExactRelease);
        }
    }

    [Flags]
    public enum VersionConstraints
    {
        None = 0,
        ExactMajor = 1,
        ExactMinor = 2,
        ExactPatch = 4,
        ExactRelease = 8
    }
}
