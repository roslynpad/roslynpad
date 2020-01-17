using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynPad.NuGet;
using RoslynPad.Runtime;

namespace RoslynPad.Build
{
    internal interface IExecutionHost
    {
        ExecutionPlatform Platform { get; set; }
        string Name { get; set; }
        string? DotNetExecutable { get; set; }
        ImmutableArray<MetadataReference> MetadataReferences { get; }
        ImmutableArray<AnalyzerFileReference> Analyzers { get; }

        event Action<IList<CompilationErrorResultObject>>? CompilationErrors;
        event Action<string>? Disassembled;
        event Action<ResultObject>? Dumped;
        event Action<ExceptionResultObject>? Error;
        event Action? ReadInput;
        event Action? RestoreStarted;
        event Action<RestoreResult>? RestoreCompleted;
        event Action<RestoreResultObject>? RestoreMessage;
        event Action<ProgressResultObject>? ProgressChanged;

        void UpdateLibraries(IList<LibraryRef> libraries);

        Task SendInputAsync(string input);
        Task ExecuteAsync(string code, bool disassemble, OptimizationLevel? optimizationLevel);
        Task TerminateAsync();
    }

    internal class RestoreResult
    {
        public static RestoreResult SuccessResult { get; } = new RestoreResult(success: true, errors: null);

        public static RestoreResult FromErrors(string[] errors) => new RestoreResult(success: false, errors);

        private RestoreResult(bool success, string[]? errors)
        {
            Success = success;
            Errors = errors ?? Array.Empty<string>();
        }

        public bool Success { get; }
        public string[] Errors { get; }
    }
}