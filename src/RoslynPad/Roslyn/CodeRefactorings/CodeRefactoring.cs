using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.CodeRefactorings
{
    public sealed class CodeRefactoring
    {
        public CodeRefactoringProvider Provider { get; }

        public IReadOnlyList<CodeAction> Actions { get; }

        public CodeRefactoring(object inner)
        {
            Provider = inner.GetPropertyValue<CodeRefactoringProvider>(nameof(Provider));
            Actions = inner.GetPropertyValue<IReadOnlyList<CodeAction>>(nameof(Actions));
        }
    }
}