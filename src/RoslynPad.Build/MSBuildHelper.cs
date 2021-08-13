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

        public static XDocument CreateCsproj(bool isCore, string targetFramework, IEnumerable<LibraryRef> references) =>
            new XDocument(
                new XElement("Project",
                    ImportSdkProject("Microsoft.NET.Sdk", "Sdk.props"),
                    BuildProperties(targetFramework),
                    References(references),
                    ReferenceAssemblies(isCore),
                    ImportSdkProject("Microsoft.NET.Sdk", "Sdk.targets"),
                    CoreCompileTarget()));

        private static XElement ReferenceAssemblies(bool isCore) =>
            isCore ? new XElement("ItemGroup") : new XElement("ItemGroup",
                new XElement("PackageReference",
                    new XAttribute("Include", "Microsoft.NETFramework.ReferenceAssemblies"),
                    new XAttribute("Version", "*")));

        private static XElement References(IEnumerable<LibraryRef> references) =>
            new XElement("ItemGroup",
                references.Select(reference =>
                {
                    var element =
                        new XElement(reference.Kind.ToString(),
                            new XAttribute("Include", reference.Value));
                    if (!string.IsNullOrEmpty(reference.Version))
                    {
                        element.Add(new XAttribute("Version", reference.Version));
                    }

                    return element;
                })
                .ToArray());

        private static ImmutableDictionary<string, string> GetNetFrameworkAssemblies()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319");
            return IOUtilities.EnumerateFiles(path, "*.dll")
                .Concat(IOUtilities.EnumerateFiles(Path.Combine(path, "WPF"), "*.dll"))
                .ToImmutableDictionary(d => Path.GetFileNameWithoutExtension(d), StringComparer.OrdinalIgnoreCase);
        }

        private static XElement BuildProperties(string targetFramework) =>
            new XElement("PropertyGroup",
                new XElement("TargetFramework", targetFramework),
                new XElement("OutputType", "Exe"),
                new XElement("OutputPath", "bin"),
                new XElement("UseAppHost", false),
                new XElement("AppendTargetFrameworkToOutputPath", false),
                new XElement("AppendRuntimeIdentifierToOutputPath", false),
                new XElement("CopyBuildOutputToOutputDirectory", false),
                new XElement("GenerateAssemblyInfo", false));

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
