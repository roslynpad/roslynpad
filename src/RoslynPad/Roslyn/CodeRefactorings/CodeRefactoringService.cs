using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeRefactorings
{
    [Export(typeof(ICodeRefactoringService)), Shared]
    internal sealed class CodeRefactoringService : ICodeRefactoringService
    {
        private static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.CodeRefactorings.ICodeRefactoringService, Microsoft.CodeAnalysis.Features", true);
        
        private readonly object _inner;

        [ImportingConstructor]
        public CodeRefactoringService(CompositionContext compositionContext)
        {
            _inner = compositionContext.GetExport(InterfaceType);
        }

        private static readonly Func<object, Document, TextSpan, CancellationToken, Task<IEnumerable<object>>> _getRefactoringsAsync =
            CreateGetRefactoringsAsync();

        private static Func<object, Document, TextSpan, CancellationToken, Task<IEnumerable<object>>> CreateGetRefactoringsAsync()
        {
            var param = new[]
            {
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(Document)),
                Expression.Parameter(typeof(TextSpan)),
                Expression.Parameter(typeof(CancellationToken))
            };
            var methodInfo = InterfaceType.GetMethod(nameof(GetRefactoringsAsync));
            return Expression.Lambda<Func<object, Document, TextSpan, CancellationToken, Task<IEnumerable<object>>>>
                (Expression.Call(typeof(Utilities.TaskExtensions).GetMethod(nameof(Utilities.TaskExtensions.Cast), BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments()[0], typeof(IEnumerable<object>)),
                        Expression.Call(Expression.Convert(param[0], InterfaceType), methodInfo, param[1], param[2], param[3])),
                    param).Compile();
        }

        public Task<bool> HasRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<CodeRefactoring>> GetRefactoringsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var result = await _getRefactoringsAsync(_inner, document, textSpan, cancellationToken).ConfigureAwait(false);

            return result.Select(x => new CodeRefactoring(x)).ToArray();
        }
    }
}