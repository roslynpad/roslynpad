using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn.WorkspaceServices
{
    [ExportWorkspaceService(typeof(ISymbolRenamedCodeActionOperationFactoryWorkspaceService), ServiceLayer.Host), Shared]
    internal sealed class SymbolRenamedCodeActionOperationFactory : ISymbolRenamedCodeActionOperationFactoryWorkspaceService
    {
        public CodeActionOperation CreateSymbolRenamedOperation(ISymbol symbol, string newName, Solution startingSolution, Solution updatedSolution)
        {
            // this action does nothing - but Roslyn requires it for some Code Fixes

            return new RenameSymbolOperation(
                symbol ?? throw new ArgumentNullException(nameof(symbol)),
                newName ?? throw new ArgumentNullException(nameof(newName)));
        }

        private class RenameSymbolOperation : CodeActionOperation
        {
            private readonly ISymbol _symbol;
            private readonly string _newName;

            public RenameSymbolOperation(
                ISymbol symbol,
                string newName)
            {
                _symbol = symbol;
                _newName = newName;
            }

            public override string Title => $"Rename {_symbol.Name} to {_newName}";
        }
    }
}
