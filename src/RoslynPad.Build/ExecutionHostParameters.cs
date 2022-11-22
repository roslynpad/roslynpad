using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Build;

internal class ExecutionHostParameters
{
    public ExecutionHostParameters(
        string buildPath,
        string nuGetConfigPath,
        ImmutableArray<string> imports,
        ImmutableArray<string> disabledDiagnostics,
        string workingDirectory,
        SourceCodeKind sourceCodeKind,
        bool checkOverflow = false,
        bool allowUnsafe = true)
    {
        BuildPath = buildPath;
        NuGetConfigPath = nuGetConfigPath;
        Imports = imports;
        DisabledDiagnostics = disabledDiagnostics;
        WorkingDirectory = workingDirectory;
        SourceCodeKind = sourceCodeKind;
        CheckOverflow = checkOverflow;
        AllowUnsafe = allowUnsafe;
    }

    public string BuildPath { get; }
    public string NuGetConfigPath { get; }
    public ImmutableArray<string> Imports { get; set; }
    public ImmutableArray<string> DisabledDiagnostics { get; }
    public string WorkingDirectory { get; set; }
    public SourceCodeKind SourceCodeKind { get; }
    public bool CheckOverflow { get; }
    public bool AllowUnsafe { get; }
}
