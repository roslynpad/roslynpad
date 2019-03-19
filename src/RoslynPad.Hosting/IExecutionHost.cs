using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using RoslynPad.Runtime;

namespace RoslynPad.Hosting
{
    internal interface IExecutionHost : IDisposable
    {
        ExecutionPlatform Platform { get; set; }
        PlatformVersion PlatformVersion { get; set; }
        string Name { get; set; }

        event Action<IList<CompilationErrorResultObject>> CompilationErrors;
        event Action<string> Disassembled;
        event Action<ResultObject> Dumped;
        event Action<ExceptionResultObject> Error;
        event Action ReadInput;

        Task SendInput(string input);
        Task ExecuteAsync(string code, bool disassemble, OptimizationLevel? optimizationLevel);
        Task ResetAsync();
        Task Update(ExecutionHostParameters parameters);
    }
}