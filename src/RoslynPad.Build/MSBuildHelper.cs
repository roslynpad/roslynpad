using RoslynPad.NuGet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RoslynPad.Build
{
    internal static class MSBuildHelper
    {
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
                            new XAttribute("Include", reference.Value));
                    if (!string.IsNullOrEmpty(reference.Version))
                    {
                        element.Add(new XAttribute("Version", reference.Version));
                    }

                    return element;
                })
                .ToArray());

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
                new XElement("WriteLinesToFile",
                    new XAttribute("File", "references.txt"),
                    new XAttribute("Lines", "@(ReferencePathWithRefAssemblies)"),
                    new XAttribute("Overwrite", true)));

        private static XElement ImportSdkProject(string sdk, string project) =>
            new XElement("Import",
                new XAttribute("Sdk", sdk),
                new XAttribute("Project", project));
    }
}
