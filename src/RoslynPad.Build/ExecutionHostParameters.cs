using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Build;

internal class ExecutionHostParameters(
    string buildPath,
    string nuGetConfigPath,
    ImmutableArray<string> imports,
    ImmutableHashSet<string> disabledDiagnostics,
    string workingDirectory,
    SourceCodeKind sourceCodeKind,
    bool checkOverflow = false,
    bool allowUnsafe = true)
{
    public string BuildPath { get; } = buildPath;
    public string NuGetConfigPath { get; } = nuGetConfigPath;
    public ImmutableArray<string> Imports { get; set; } = imports;
    public ImmutableHashSet<string> DisabledDiagnostics { get; } = disabledDiagnostics;
    public string WorkingDirectory { get; set; } = workingDirectory;
    public SourceCodeKind SourceCodeKind { get; set; } = sourceCodeKind;
    public bool CheckOverflow { get; } = checkOverflow;
    public bool AllowUnsafe { get; } = allowUnsafe;
}
