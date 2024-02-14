using System.Xml.Linq;

namespace RoslynPad.Build;

internal static class MSBuildHelper
{
    public const string ReferencesFile = "references.txt";
    public const string AnalyzersFile = "analyzers.txt";
    private const string Sdk = "Microsoft.NET.Sdk";

    public static XDocument CreateCsxCsproj(bool isDotNet, string targetFramework, IEnumerable<LibraryRef> references) =>
        new(new XElement("Project",
            ImportSdkProject(Sdk, "Sdk.props"),
            BuildProperties(targetFramework, copyBuildOutput: false),
            Reference(references),
            ReferenceAssemblies(isDotNet),
            ImportSdkProject(Sdk, "Sdk.targets"),
            CoreCompileTarget()));

    public static XDocument CreateCsproj(string targetFramework, IEnumerable<LibraryRef> referenceItems, IEnumerable<string> usingItems) =>
       new(new XElement("Project",
            new XAttribute("Sdk", Sdk),
            BuildProperties(targetFramework, copyBuildOutput: true),
            Reference(referenceItems),
            Using(usingItems)));

    private static XElement ReferenceAssemblies(bool isDotNet) =>
        isDotNet ? new XElement("ItemGroup") : new XElement("ItemGroup",
            new XElement("PackageReference",
                new XAttribute("Include", "Microsoft.NETFramework.ReferenceAssemblies"),
                new XAttribute("Version", "*")));

    private static XElement Reference(IEnumerable<LibraryRef> referenceItems) =>
        new("ItemGroup",
            referenceItems.Select(Reference).ToArray());

    private static XElement Reference(LibraryRef reference)
    {
        var element = new XElement(reference.Kind.ToString(),
            new XAttribute("Include", reference.Value));

        if (!string.IsNullOrEmpty(reference.Version))
        {
            element.Add(new XAttribute("Version", reference.Version));
        }

        return element;
    }

    private static XElement Compile(IEnumerable<string> compileItems) =>
        new("ItemGroup",
            compileItems.Select(c => new XElement("Compile", new XAttribute("Include", c))));

    private static XElement Using(IEnumerable<string> usingItems) =>
        new("ItemGroup",
            usingItems.Select(c => new XElement("Using", new XAttribute("Include", c))));

    private static XElement BuildProperties(string targetFramework, bool copyBuildOutput) =>
        new("PropertyGroup",
            new XElement("TargetFramework", targetFramework),
            new XElement("OutputType", "Exe"),
            new XElement("OutputPath", "bin"),
            new XElement("UseAppHost", false),
            new XElement("AllowUnsafeBlocks", true),
            new XElement("LangVersion", "preview"),
            new XElement("Nullable", "enable"),
            new XElement("AppendTargetFrameworkToOutputPath", false),
            new XElement("AppendRuntimeIdentifierToOutputPath", false),
            new XElement("AppendPlatformToOutputPath", false),
            new XElement("CopyBuildOutputToOutputDirectory", copyBuildOutput),
            new XElement("GenerateAssemblyInfo", false));

    private static XElement CoreCompileTarget() =>
        new("Target",
            new XAttribute("Name", "CoreCompile"),
            WriteLinesToFile(ReferencesFile, "@(ReferencePathWithRefAssemblies)"),
            WriteLinesToFile(AnalyzersFile, "@(Analyzer)"));

    private static XElement WriteLinesToFile(string file, string lines) =>
        new("WriteLinesToFile",
            new XAttribute("File", file),
            new XAttribute("Lines", lines),
            new XAttribute("Overwrite", true));

    private static XElement ImportSdkProject(string sdk, string project) =>
        new("Import",
            new XAttribute("Sdk", sdk),
            new XAttribute("Project", project));
}
