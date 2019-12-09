using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace RoslynPad.Roslyn.CodeRefactorings
{
    public sealed class CodeRefactoring
    {
        public CodeRefactoringProvider Provider { get; }

        public ImmutableArray<CodeAction> Actions { get; }

        internal CodeRefactoring(Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoring inner)
        {
            Provider = inner.Provider;
            Actions = inner.CodeActions.Select(c => c.action).ToImmutableArray();
        }
    }
}