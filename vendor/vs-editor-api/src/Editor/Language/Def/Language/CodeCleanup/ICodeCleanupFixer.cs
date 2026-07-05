using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Fixer that can fix issues to clean up code
    /// </summary>
    public interface ICodeCleanUpFixer
    {
        /// <summary>
        /// Fix issues in the files identified by the scope
        /// </summary>
        /// <param name="scope">Context to fix issues within</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>bool if the fixer succeeded false otherwise</returns>
        Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken);
    }
}
