using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using RoslynPad.Roslyn;
using RoslynPad.Runtime;

namespace RoslynPad.Hosting 
{
    internal interface IExecutionHost : IDisposable 
    {
        Platform Platform { get; set; }

        Task<ExceptionResultObject> ExecuteAsync(string code, CancellationToken ct = default(CancellationToken));

        Task CompileAndSave(string code, string assemblyPath);

        event Action<IList<ResultObject>> Dumped;

        Task ResetAsync();
    }
}
