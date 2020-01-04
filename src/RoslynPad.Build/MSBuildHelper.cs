using RoslynPad.NuGet;
using RoslynPad.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RoslynPad.Build
{
    internal static class MSBuildHelper
    {
        public const string ReferencesFile = "references.txt";
        public const string AnalyzersFile = "analyzers.txt";

        public static XDocument CreateCsproj(string targetFramework, IEnumerable<LibraryRef> references) =>
            new XDocument(
                new XElement("Project",
                    ImportSdkProject("Microsoft.NET.Sdk", "Sdk.props"),
                    BuildProperties(targetFramework),
                    References(references),
                    ImportSdkProject("Microsoft.NET.Sdk", "Sdk.targets"),
                    CoreCompileTarget()));

        private static XElement References(IEnumerable<LibraryRef> references) =>
            new XElement("ItemGroup",
                references.Select(reference =>
                {
                    var element =
                        new XElement(reference.Kind.ToString(),
                            new XAttribute("Include", GetReferenceInclude(reference)));
                    if (!string.IsNullOrEmpty(reference.Version))
                    {
                        element.Add(new XAttribute("Version", reference.Version));
                    }

                    return element;
                })
                .ToArray());

        private static readonly Lazy<ImmutableDictionary<string, string>> _netFrameworkAssemblies =
            new Lazy<ImmutableDictionary<string, string>>(GetNetFrameworkAssemblies);

        private static ImmutableDictionary<string, string> GetNetFrameworkAssemblies()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319");
            return IOUtilities.EnumerateFiles(path, "*.dll")
                .Concat(IOUtilities.EnumerateFiles(Path.Combine(path, "WPF"), "*.dll"))
                .ToImmutableDictionary(d => Path.GetFileNameWithoutExtension(d), StringComparer.OrdinalIgnoreCase);
        }

        private static string GetReferenceInclude(LibraryRef reference)
        {
            if (reference.Kind == LibraryRef.RefKind.Reference &&
                IsGacReference(reference.Value) &&
                _netFrameworkAssemblies.Value.TryGetValue(reference.Value, out var referenceAssembly))
            {
                return referenceAssembly;
            }

            return reference.Value;

            static bool IsGacReference(string name)
            {
                switch (Path.GetExtension(name)?.ToLowerInvariant())
                {
                    case ".dll":
                    case ".exe":
                    case ".winmd":
                        return false;
                    default:
                        return true;
                }
            }
        }

        private static XElement BuildProperties(string targetFramework)
        {
            var group = new XElement("PropertyGroup",
                new XElement("TargetFramework", targetFramework),
                new XElement("OutputType", "Exe"),
                new XElement("OutputPath", "bin"),
                new XElement("UseAppHost", false),
                new XElement("AppendTargetFrameworkToOutputPath", false),
                new XElement("AppendRuntimeIdentifierToOutputPath", false),
                new XElement("CopyBuildOutputToOutputDirectory", false),
                new XElement("GenerateAssemblyInfo", false));

            if (!targetFramework.Contains("core", StringComparison.OrdinalIgnoreCase))
            {
                group.Add(new XElement("FrameworkPathOverride", @"$(WinDir)\Microsoft.NET\Framework\v4.0.30319"));
            }

            return group;
        }

        private static XElement CoreCompileTarget() =>
            new XElement("Target",
                new XAttribute("Name", "CoreCompile"),
                WriteLinesToFile(ReferencesFile, "@(ReferencePathWithRefAssemblies)"),
                WriteLinesToFile(AnalyzersFile, "@(Analyzer)"));

        private static XElement WriteLinesToFile(string file, string lines) =>
            new XElement("WriteLinesToFile",
                new XAttribute("File", file),
                new XAttribute("Lines", lines),
                new XAttribute("Overwrite", true));

        private static XElement ImportSdkProject(string sdk, string project) =>
            new XElement("Import",
                new XAttribute("Sdk", sdk),
                new XAttribute("Project", project));
    }
}
