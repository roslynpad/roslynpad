using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;

namespace RoslynPad.Roslyn;

public class DocumentCreationArgs(SourceTextContainer sourceTextContainer, string workingDirectory, SourceCodeKind sourceCodeKind, Action<DiagnosticsUpdatedArgs>? onDiagnosticsUpdated = null, Action<SourceText>? onTextUpdated = null, string? name = null)
{
    public SourceTextContainer SourceTextContainer { get; } = sourceTextContainer;
    public string WorkingDirectory { get; } = workingDirectory;
    public SourceCodeKind SourceCodeKind { get; } = sourceCodeKind;
    public Action<DiagnosticsUpdatedArgs>? OnDiagnosticsUpdated { get; } = onDiagnosticsUpdated;
    public Action<SourceText>? OnTextUpdated { get; } = onTextUpdated;
    public string? Name { get; } = name;
}
