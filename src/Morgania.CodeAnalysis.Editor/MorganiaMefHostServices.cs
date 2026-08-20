// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Morgania.CodeAnalysis.Editor;

public sealed class MorganiaMefHostServices : HostServices, IMefHostExportProvider
{
    private readonly ExportProvider _exportProvider;
    private ImmutableDictionary<ExportKey, IEnumerable> _exportsMap =
        ImmutableDictionary<ExportKey, IEnumerable>.Empty;

    private MorganiaMefHostServices(ExportProvider exportProvider)
    {
        ArgumentNullException.ThrowIfNull(exportProvider);
        _exportProvider = exportProvider;
    }

    public static MorganiaMefHostServices Create(ExportProvider exportProvider) => new(exportProvider);

    public override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace) =>
        new MefWorkspaceServices(this, workspace);

    public IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>()
    {
        var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName!, typeof(TMetadata).AssemblyQualifiedName!);
        var exports = ImmutableInterlocked.GetOrAdd(
            ref _exportsMap,
            key,
            _ => _exportProvider.GetExports<TExtension, TMetadata>().ToImmutableArray());

        return (IEnumerable<Lazy<TExtension, TMetadata>>)exports;
    }

    public IEnumerable<Lazy<TExtension>> GetExports<TExtension>()
    {
        var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName!, "");
        var exports = ImmutableInterlocked.GetOrAdd(
            ref _exportsMap,
            key,
            _ => _exportProvider.GetExports<TExtension>().ToImmutableArray());

        return (IEnumerable<Lazy<TExtension>>)exports;
    }

    private readonly struct ExportKey : IEquatable<ExportKey>
    {
        private readonly string _extensionTypeName;
        private readonly string _metadataTypeName;

        public ExportKey(string extensionTypeName, string metadataTypeName)
        {
            _extensionTypeName = extensionTypeName;
            _metadataTypeName = metadataTypeName;
        }

        public bool Equals(ExportKey other) =>
            StringComparer.OrdinalIgnoreCase.Equals(_extensionTypeName, other._extensionTypeName) &&
            StringComparer.OrdinalIgnoreCase.Equals(_metadataTypeName, other._metadataTypeName);

        public override bool Equals(object? obj) => obj is ExportKey key && Equals(key);

        public override int GetHashCode() =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(_extensionTypeName),
                StringComparer.OrdinalIgnoreCase.GetHashCode(_metadataTypeName));
    }
}
