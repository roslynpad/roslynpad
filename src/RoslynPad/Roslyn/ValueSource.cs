using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Roslyn
{
    internal static class ValueSource
    {
        internal static readonly Type Type = Type.GetType("Roslyn.Utilities.ValueSource`1, Microsoft.CodeAnalysis.Workspaces", throwOnError: true);
    }

    internal static class ValueSource<T>
    {
        private static readonly Func<object, CancellationToken, Task<T>> _getValueAsync = CreateGetValueAsyncFunc();

        private static Func<object, CancellationToken, Task<T>> CreateGetValueAsyncFunc()
        {
            var type = ValueSource.Type.MakeGenericType(typeof(T));

            var param = new[]
            {
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(CancellationToken))
            };
            return Expression.Lambda<Func<object, CancellationToken, Task<T>>>(
                Expression.Call(Expression.Convert(param[0], type), type.GetMethod(nameof(GetValueAsync)), param[1]),
                param).Compile();
        }

        public static Task<T> GetValueAsync(object valueSource, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _getValueAsync(valueSource, cancellationToken);
        }
    }
}