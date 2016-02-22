using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;

namespace RoslynPad.Roslyn.Completion
{
    public static class CompletionService
    {
        private static readonly Type _innerType = Type.GetType("Microsoft.CodeAnalysis.Completion.CompletionService, Microsoft.CodeAnalysis.Features", throwOnError: true);

        private static readonly Func<Document, int, object, OptionSet, IEnumerable<object>, CancellationToken, Task<object>>
            _getCompletionListAsync = CreateGetCompletionListAsyncFunc();

        private static Func<Document, int, object, OptionSet, IEnumerable<object>, CancellationToken, Task<object>> CreateGetCompletionListAsyncFunc()
        {
            var innerMethod = _innerType.GetMethod(nameof(GetCompletionListAsync), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var innerMethodsParam = innerMethod.GetParameters();
            var param = new[]
            {
                Expression.Parameter(typeof(Document)),
                Expression.Parameter(typeof(int)),
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(OptionSet)),
                Expression.Parameter(typeof(IEnumerable<object>)),
                Expression.Parameter(typeof(CancellationToken))
            };
            return Expression.Lambda<Func<Document, int, object, OptionSet, IEnumerable<object>, CancellationToken, Task<object>>>(
                Expression.Call(typeof(Utilities.TaskExtensions).GetMethod(nameof(Utilities.TaskExtensions.Cast))
                    .MakeGenericMethod(innerMethod.ReturnType.GetGenericArguments()[0], typeof(object)),
                    Expression.Call(innerMethod,
                        param[0], param[1], Expression.Convert(param[2], innerMethodsParam[2].ParameterType),
                        param[3], Expression.Convert(param[4], innerMethodsParam[4].ParameterType), param[5])), param).Compile();
        }

        private static readonly Func<Document, int, IEnumerable<object>, CancellationToken, Task<bool>>
           _isCompletionTriggerCharacterAsync = CreateIsCompletionTriggerCharacterAsync();

        private static Func<Document, int, IEnumerable<object>, CancellationToken, Task<bool>> CreateIsCompletionTriggerCharacterAsync()
        {
            var innerMethod = _innerType.GetMethod(nameof(IsCompletionTriggerCharacterAsync), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var innerMethodsParam = innerMethod.GetParameters();
            var param = new[]
            {
                Expression.Parameter(typeof(Document)),
                Expression.Parameter(typeof(int)),
                Expression.Parameter(typeof(IEnumerable<object>)),
                Expression.Parameter(typeof(CancellationToken))
            };
            return Expression.Lambda<Func<Document, int, IEnumerable<object>, CancellationToken, Task<bool>>>(
                Expression.Call(innerMethod,
                        param[0], param[1], Expression.Convert(param[2], innerMethodsParam[2].ParameterType),
                        param[3]),
                param).Compile();
        }


        private static readonly Func<Document, object> _getCompletionRules = CreateGetCompletionRules();

        private static Func<Document, object> CreateGetCompletionRules()
        {
            var innerMethod = _innerType.GetMethod(nameof(GetCompletionRules), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var param = new[]
            {
                Expression.Parameter(typeof(Document))
            };
            return Expression.Lambda<Func<Document, object>>(
                Expression.Convert(Expression.Call(innerMethod, param[0]), typeof(object)),
                param).Compile();
        }

        public static CompletionRules GetCompletionRules(Document document)
        {
            var rules = _getCompletionRules(document);
            return new CompletionRules(rules);
        }

        public static async Task<CompletionList> GetCompletionListAsync(Document document, int position,
            CompletionTriggerInfo triggerInfo, OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = await _getCompletionListAsync(document, position, triggerInfo.Inner, options, null,
                cancellationToken).ConfigureAwait(false);
            return list == null ? null : new CompletionList(list);
        }

        public static Task<bool> IsCompletionTriggerCharacterAsync(Document document, int characterPosition,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _isCompletionTriggerCharacterAsync(document, characterPosition, null, cancellationToken);
        }
    }
}