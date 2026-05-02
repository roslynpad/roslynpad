using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace RoslynPad.Roslyn.CodeActions;

public static class CodeActionOperationExtensions
{
    public static Task<bool> TryApplyAsync(this CodeActionOperation operation, Workspace workspace, CancellationToken cancellationToken)
    {
        return operation.TryApplyAsync(workspace, workspace.CurrentSolution, CodeAnalysisProgress.None, cancellationToken);
    }
}
