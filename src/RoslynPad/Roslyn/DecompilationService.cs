using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.DecompiledSource;
using Microsoft.CodeAnalysis.DecompiledSource;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn;

/// <summary>
/// Exposes the ILSpy-backed decompiler that ships in Microsoft.CodeAnalysis.LanguageServer.Protocol
/// (referenced, but deliberately not part-scanned into the MEF catalog) so metadata-as-source
/// can decompile symbols without Source Link/PDB sources.
/// </summary>
[ExportLanguageService(typeof(IDecompilationService), LanguageNames.CSharp), Shared]
internal sealed class DecompilationService : IDecompilationService
{
    // The inner service's constructor is [Obsolete(error: true)] (MEF-only); Activator bypasses that.
    private readonly CSharpDecompilationService _inner = Activator.CreateInstance<CSharpDecompilationService>();

    public Document? PerformDecompilation(Document document, string fullName, Compilation compilation, MetadataReference? metadataReference, string? assemblyLocation) =>
        _inner.PerformDecompilation(document, fullName, compilation, metadataReference, assemblyLocation);

    public FileVersionInfo GetDecompilerVersion() => _inner.GetDecompilerVersion();
}
