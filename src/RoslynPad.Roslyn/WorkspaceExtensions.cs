using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn;

/// <summary>
/// Provide a way for users to turn on and off analyzing workspace for compiler diagnostics
/// </summary>
public static class WorkspaceExtensions
{
    public static void EnableDiagnostics(this Workspace workspace)
    {
        DiagnosticProvider.Enable(workspace);
    }

    public static void DisableDiagnostics(this Workspace workspace)
    {
        DiagnosticProvider.Disable(workspace);
    }
}
