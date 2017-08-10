using System;
using System.IO;

namespace RoslynPad.Roslyn
{
    internal static class NuGetConfigurationExtensions
    {
        public static string ResolveReference(this NuGetConfiguration configuration, string reference)
        {
            if (configuration?.PathVariableName != null &&
                configuration.PathToRepository != null &&
                reference.StartsWith(configuration.PathVariableName, StringComparison.OrdinalIgnoreCase))
            {
                reference = Path.Combine(configuration.PathToRepository,
                    reference.Substring(configuration.PathVariableName.Length).TrimStart('/', '\\'));
            }
            return reference;
        }
    }
}