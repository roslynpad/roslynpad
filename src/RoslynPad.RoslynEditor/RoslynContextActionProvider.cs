using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
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
        private static readonly ImmutableArray<string> ExcludedRefactoringProviders =
            ImmutableArray.Create("ExtractInterface");

        private readonly DocumentId _documentId;
        private readonly RoslynHost _roslynHost;
        private readonly ICodeFixService _codeFixService;

        public RoslynContextActionProvider(DocumentId documentId, RoslynHost roslynHost)
        {
            _documentId = documentId;
            _roslynHost = roslynHost;
            _codeFixService = _roslynHost.GetService<ICodeFixService>();
        }

        public async Task<IEnumerable<object>> GetActions(int offset, int length, CancellationToken cancellationToken)
        {
            var textSpan = new TextSpan(offset, length);
            var document = _roslynHost.GetDocument(_documentId);
            var codeFixes = await _codeFixService.GetFixesAsync(document,
                textSpan, false, cancellationToken).ConfigureAwait(false);

            var codeRefactorings = await _roslynHost.GetService<ICodeRefactoringService>().GetRefactoringsAsync(document,
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
                operation.Apply(_roslynHost.GetDocument(_documentId).Project.Solution.Workspace, CancellationToken.None);
            }
        }
    }
}