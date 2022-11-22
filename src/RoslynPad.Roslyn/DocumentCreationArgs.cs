using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Roslyn.Diagnostics;

namespace RoslynPad.Roslyn;

public class DocumentCreationArgs
{
    public DocumentCreationArgs(SourceTextContainer sourceTextContainer, string workingDirectory, SourceCodeKind sourceCodeKind, Action<DiagnosticsUpdatedArgs>? onDiagnosticsUpdated = null, Action<SourceText>? onTextUpdated = null, string? name = null)
    {
        SourceTextContainer = sourceTextContainer;
        WorkingDirectory = workingDirectory;
        SourceCodeKind = sourceCodeKind;
        OnDiagnosticsUpdated = onDiagnosticsUpdated;
        OnTextUpdated = onTextUpdated;
        Name = name;
    }

    public SourceTextContainer SourceTextContainer { get; }
    public string WorkingDirectory { get; }
    public SourceCodeKind SourceCodeKind { get; }
    public Action<DiagnosticsUpdatedArgs>? OnDiagnosticsUpdated { get; }
    public Action<SourceText>? OnTextUpdated { get; }
    public string? Name { get; }
}
