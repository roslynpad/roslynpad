using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace RoslynPad.Roslyn.CodeRefactorings
{
    public sealed class CodeRefactoring
    {
        public CodeRefactoringProvider Provider { get; }

        public IReadOnlyList<CodeAction> Actions { get; }

        internal CodeRefactoring(Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoring inner)
        {
            Provider = inner.Provider;
            Actions = inner.Actions;
        }
    }
}