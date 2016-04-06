using System;
using System.IO;

namespace RoslynPad.Roslyn
{
    internal static class NuGetProviderExtensions
    {
        public static string ResolveReference(this INuGetProvider nuGetProvider, string reference)
        {
            if (nuGetProvider?.PathVariableName != null &&
                nuGetProvider.PathToRepository != null &&
                reference.StartsWith(nuGetProvider.PathVariableName, StringComparison.OrdinalIgnoreCase))
            {
                reference = Path.Combine(nuGetProvider.PathToRepository,
                    reference.Substring(nuGetProvider.PathVariableName.Length).TrimStart('/', '\\'));
            }
            return reference;
        }
    }
}