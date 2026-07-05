using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;

namespace Morgania.Demo.EditorFeatures;

/// <summary>
/// Creates the Roslyn side of the demo: a workspace over the shared VS MEF graph, a project
/// with framework references, and a document opened over the editor buffer — the same flow as
/// RoslynPad's RoslynHost.AddDocument.
/// </summary>
internal static class RoslynDemoHost
{
    public static DemoWorkspace OpenDocument(ExportProvider exportProvider, ITextBuffer buffer, string documentName)
    {
        var hostServices = VisualStudioMefHostServices.Create(exportProvider);
        var workspace = new DemoWorkspace(hostServices);

        var projectId = ProjectId.CreateNewId("RoslynPadDemo");
        var documentId = DocumentId.CreateNewId(projectId, documentName);

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name: "RoslynPadDemo",
            assemblyName: "RoslynPadDemo",
            language: LanguageNames.CSharp,
            parseOptions: new CSharpParseOptions(languageVersion: LanguageVersion.Preview, preprocessorSymbols: ["DEBUG"]),
            compilationOptions: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable),
            metadataReferences: GetFrameworkReferences());

        var solution = workspace.CurrentSolution
            .WithAnalyzerReferences(GetAnalyzerReferences())
            .AddProject(projectInfo)
            .AddDocument(documentId, documentName, buffer.CurrentSnapshot.AsText(), filePath: Path.Combine(Path.GetTempPath(), documentName));

        workspace.SetSolution(solution);
        workspace.OpenDocumentInBuffer(documentId, buffer);

        return workspace;
    }

    /// <summary>
    /// Solution-level analyzer references become the workspace's "host analyzers" — the source
    /// of compiler and IDE diagnostics for the pull-diagnostics taggers (same set RoslynPad's
    /// RoslynHost registers).
    /// </summary>
    private static IEnumerable<AnalyzerReference> GetAnalyzerReferences()
    {
        var loader = new AnalyzerAssemblyLoader();
        return new[]
        {
            "Microsoft.CodeAnalysis",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CodeAnalysis.Features",
            "Microsoft.CodeAnalysis.CSharp.Features",
        }.Select(name => (AnalyzerReference)new AnalyzerFileReference(Assembly.Load(name).Location, loader));
    }

    /// <summary>The analyzer assemblies are already loaded (they are demo dependencies).</summary>
    private sealed class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
    }

    private static IEnumerable<MetadataReference> GetFrameworkReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(static path => Path.GetFileName(path) is var file &&
            (file.StartsWith("System.", StringComparison.Ordinal) ||
             file is "mscorlib.dll" or "netstandard.dll" or "Microsoft.CSharp.dll"))
        .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path));
}
