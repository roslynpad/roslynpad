﻿using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RoslynPad.Roslyn.Diagnostics;

[Export(typeof(IDiagnosticsRefresher))]
internal class NullDiagnosticsRefresher : IDiagnosticsRefresher
{
    public int GlobalStateVersion { get; }

    public event Action? WorkspaceRefreshRequested;

    public void RequestWorkspaceRefresh()
    {
        WorkspaceRefreshRequested?.Invoke();
    }
}
