using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.IO;
using Roslyn.Utilities;
using System.Runtime.InteropServices;

namespace RoslynPad.Roslyn
{
    internal class MetadataUtil
    {
        public static IReadOnlyList<Type> LoadTypesByNamespaces(Assembly assembly, params string[] namespaces)
        {
            using var context = new MetadataLoadContext(new PathAssemblyResolver(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")));
            var types = context.LoadFromAssemblyPath(assembly.Location).DefinedTypes;
            return types
                .Where(t => namespaces.Contains(t.Namespace))
                .Select(t => assembly.GetType(t.FullName!)).WhereNotNull().ToArray();
        }
    }
}
