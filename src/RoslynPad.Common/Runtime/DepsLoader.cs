using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace RoslynPad.Runtime
{
    /// <summary>
    /// Loads assemblies using a deps JSON file on .NET Framework.
    /// </summary>
    internal class DepsLoader
    {
        private readonly IReadOnlyDictionary<string, string> _assemblyPaths;
        private readonly ConcurrentDictionary<string, Assembly?> _assemblies;

        public DepsLoader(string depsFile)
        {
            _assemblies = new ConcurrentDictionary<string, Assembly?>();

            var nugetPath = GetWindowsNuGetPath();

            using (var depsParser = new DepsParser(depsFile, nugetPath))
            {
                _assemblyPaths = depsParser.ParseRuntimeAssemblies();
            }
        }

        public Assembly? TryLoadAssembly(string name)
        {
            return _assemblies.GetOrAdd(name, assemblyName =>
            {
                var commaIndex = assemblyName.IndexOf(',');
                if (commaIndex > 0)
                {
                    assemblyName = assemblyName.Substring(0, commaIndex);
                }

                if (_assemblyPaths.TryGetValue(assemblyName, out var path))
                {
                    return Assembly.Load(AssemblyName.GetAssemblyName(path));
                }

                return null;
            });
        }

        private static string GetWindowsNuGetPath()
        {
            var nugetPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

            if (string.IsNullOrEmpty(nugetPath))
            {
                nugetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            }

            return nugetPath;
        }
    }
}
