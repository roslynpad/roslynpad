using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.IO;
using Roslyn.Utilities;
using System.Runtime.InteropServices;

namespace RoslynPad.Roslyn;

internal class MetadataUtil
{
    public static IReadOnlyList<Type> LoadTypesByNamespaces(Assembly assembly, params string[] namespaces) =>
        LoadTypesBy(assembly, t => namespaces.Contains(t.Namespace));

    public static IReadOnlyList<Type> LoadTypesBy(Assembly assembly, Func<Type, bool> predicate)
    {
        using var context = new MetadataLoadContext(new PathAssemblyResolver(Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")));
        var types = context.LoadFromAssemblyPath(assembly.Location).DefinedTypes;
        return types.Where(predicate).Select(t => assembly.GetType(t.FullName!)).WhereNotNull().ToReadOnlyCollection();
    }
}
