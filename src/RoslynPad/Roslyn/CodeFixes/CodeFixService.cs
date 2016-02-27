using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.CodeFixes
{
    public sealed class CodeFixService : ICodeFixService
    {
        private static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.CodeFixes.ICodeFixService, Microsoft.CodeAnalysis.EditorFeatures", true);

        public static ICodeFixService Load(CompositionHost host)
        {
            var service = host.GetExport(InterfaceType);
            return new CodeFixService(service);
        }

        private readonly object _inner;

        private CodeFixService(object inner)
        {
            _inner = inner;
        }

        private static readonly Func<object, Document, TextSpan, bool, CancellationToken, Task<IEnumerable<object>>> _getFixesAsync =
            CreateGetFixesAsync();

        private static Func<object, Document, TextSpan, bool, CancellationToken, Task<IEnumerable<object>>> CreateGetFixesAsync()
        {
            var param = new[]
            {
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(Document)),
                Expression.Parameter(typeof(TextSpan)),
                Expression.Parameter(typeof(bool)),
                Expression.Parameter(typeof(CancellationToken))
            };
            var methodInfo = InterfaceType.GetMethod(nameof(GetFixesAsync));
            return Expression.Lambda<Func<object, Document, TextSpan, bool, CancellationToken, Task<IEnumerable<object>>>>
                (Expression.Call(typeof(Utilities.TaskExtensions).GetMethod(nameof(Utilities.TaskExtensions.Cast), BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments()[0], typeof(IEnumerable<object>)),
                        Expression.Call(Expression.Convert(param[0], InterfaceType), methodInfo, param[1], param[2], param[3], param[4])),
                    param).Compile();
        }

        public async Task<IEnumerable<CodeFixCollection>> GetFixesAsync(Document document, TextSpan textSpan, bool includeSuppressionFixes,
            CancellationToken cancellationToken)
        {
            var result = await _getFixesAsync(_inner, document, textSpan, includeSuppressionFixes, cancellationToken).ConfigureAwait(false);
            return result.Select(x => new CodeFixCollection(x)).ToImmutableArray();
        }

        public Task<FirstDiagnosticResult> GetFirstDiagnosticWithFixAsync(Document document, TextSpan textSpan, bool considerSuppressionFixes,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public CodeFixProvider GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
        {
            throw new NotImplementedException();
        }
    }
}