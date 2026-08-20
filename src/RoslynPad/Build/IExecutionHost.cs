using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RoslynPad.Build;

internal interface IExecutionHost
{
    ExecutionPlatform Platform { get; set; }
    bool UseFileBasedReferences { get; }
    string Name { get; set; }
    string DotNetExecutable { get; set; }
    ImmutableArray<MetadataReference> MetadataReferences { get; }
    ImmutableArray<AnalyzerFileReference> Analyzers { get; }
    ImmutableArray<UsingItem> Usings { get; }
    DocumentId? DocumentId { get; set; }

    event Action<IList<CompilationErrorResultObject>>? CompilationErrors;
    event Action<string>? Disassembled;
    event Action<ResultObject>? Dumped;
    event Action<ExceptionResultObject>? Error;
    event Action? ReadInput;
    event Action? RestoreStarted;
    event Action<RestoreResult>? RestoreCompleted;
    event Action<ProgressResultObject>? ProgressChanged;

    /// <summary>
    /// Supplies the sink build output streams into, called once per phase as it starts producing
    /// output (the flag indicates a cached replay); the writer is disposed when the phase ends.
    /// Called from background threads.
    /// </summary>
    Func<BuildOutputSource, bool, TextWriter>? BuildOutputWriterFactory { get; set; }

    void ClearRestoreCache();
    Task UpdateReferencesAsync(bool alwaysRestore);
    Task SendInputAsync(string input);
    Task ExecuteAsync(string path, bool disassemble, OptimizationLevel? optimizationLevel, CancellationToken cancellationToken);
    Task TerminateAsync();
}
