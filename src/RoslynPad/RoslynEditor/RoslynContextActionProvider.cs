using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.CodeActions;
using RoslynPad.Roslyn.CodeFixes;
using RoslynPad.Roslyn.CodeRefactorings;
using RoslynPad.Utilities;

namespace RoslynPad.RoslynEditor
{
    internal sealed class RoslynContextActionProvider : IContextActionProvider
    {
        private readonly RoslynHost _roslynHost;

        private static readonly ImmutableArray<string> ExcludedRefactoringProviders =
            ImmutableArray.Create("ExtractInterface");

        public RoslynContextActionProvider(RoslynHost roslynHost)
        {
            _roslynHost = roslynHost;
        }

        public async Task<IEnumerable<object>> GetActions(int offset, int length, CancellationToken cancellationToken)
        {
            var textSpan = new TextSpan(offset, length);
            var codeFixes = await _roslynHost.GetService<ICodeFixService>().GetFixesAsync(_roslynHost.CurrentDocument,
                textSpan, false, cancellationToken).ConfigureAwait(false);

            var codeRefactorings = await _roslynHost.GetService<ICodeRefactoringService>().GetRefactoringsAsync(_roslynHost.CurrentDocument,
                textSpan, cancellationToken).ConfigureAwait(false);

            return ((IEnumerable<object>)codeFixes.SelectMany(x => x.Fixes))
                .Concat(codeRefactorings
                    .Where(x => ExcludedRefactoringProviders.All(p => !x.Provider.GetType().Name.Contains(p)))
                    .SelectMany(x => x.Actions));
        }
        public ICommand GetActionCommand(object action)
        {
            var codeAction = action as CodeAction;
            if (codeAction != null)
            {
                return new DelegateCommand(() => ExecuteCodeAction(codeAction));
            }
            var codeFix = action as CodeFix;
            if (codeFix == null || codeFix.Action.HasCodeActions()) return null;
            return new DelegateCommand(() => ExecuteCodeAction(codeFix.Action));
        }

        private async Task ExecuteCodeAction(CodeAction codeAction)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(true);
            foreach (var operation in operations)
            {
                operation.Apply(_roslynHost.Workspace, CancellationToken.None);
            }
        }
    }
}