using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Roslyn
{
    internal static class ValueSource<T>
    {
        private static readonly Func<object, CancellationToken, Task<T>> _getValueAsync = CreateGetValueAsyncFunc();

        private static Func<object, CancellationToken, Task<T>> CreateGetValueAsyncFunc()
        {
            var type = Type.GetType("Roslyn.Utilities.ValueSource`1, Microsoft.CodeAnalysis.Workspaces", throwOnError: true);
            type = type.MakeGenericType(typeof(T));

            var param = new[]
            {
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(CancellationToken))
            };
            return Expression.Lambda<Func<object, CancellationToken, Task<T>>>(
                Expression.Call(Expression.Convert(param[0], type), type.GetMethod("GetValueAsync"), param[1]),
                param).Compile();
        }

        public static Task<T> GetValueAsync(object valueSource, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _getValueAsync(valueSource, cancellationToken);
        }
    }
}