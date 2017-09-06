using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace RoslynPad.Roslyn
{
    public class NuGetScriptMetadataResolver : MetadataReferenceResolver
    {
        private readonly NuGetConfiguration _nuGetConfiguration;
        private readonly ScriptMetadataResolver _inner;
        private readonly ConcurrentDictionary<string, ImmutableArray<PortableExecutableReference>> _cache;

        public NuGetScriptMetadataResolver(NuGetConfiguration nuGetConfiguration, string workingDirectory, bool useCache = false)
        {
            _nuGetConfiguration = nuGetConfiguration;
            _inner = ScriptMetadataResolver.Default.WithBaseDirectory(workingDirectory);
            if (useCache)
            {
                _cache = new ConcurrentDictionary<string, ImmutableArray<PortableExecutableReference>>();
            }
        }

        public override bool Equals(object other) => _inner.Equals(other);

        public override int GetHashCode() => _inner.GetHashCode();

        public override bool ResolveMissingAssemblies => _inner.ResolveMissingAssemblies;

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            if (_cache == null)
            {
                return _inner.ResolveMissingAssembly(definition, referenceIdentity);
            }
            
            return _cache.GetOrAdd(referenceIdentity.ToString(),
                _ => ImmutableArray.Create(_inner.ResolveMissingAssembly(definition, referenceIdentity))).FirstOrDefault();
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            reference = NuGetConfigurationExtensions.ResolveReference(_nuGetConfiguration, reference);

            if (_cache == null)
            {
                return _inner.ResolveReference(reference, baseFilePath, properties);
            }

            if (!_cache.TryGetValue(reference, out var result))
            {
                result = _inner.ResolveReference(reference, baseFilePath, properties);
                if (!result.IsDefaultOrEmpty)
                {
                    _cache.TryAdd(reference, result);
                }
            }

            return result;
        }
    }
}