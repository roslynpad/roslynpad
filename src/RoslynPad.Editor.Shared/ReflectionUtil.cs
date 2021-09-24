using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RoslynPad.Editor
{
    internal static class ReflectionUtil
    {
        /// <summary>
        /// Allows accessing private fields efficiently.
        /// </summary>
        /// <typeparam name="TOwner">Type of the field's owner.</typeparam>
        /// <typeparam name="TField">Type of the field.</typeparam>
        /// <param name="fieldName">The field name.</param>
        /// <returns>A delegate field accessor.</returns>
        internal static Func<TOwner, TField> GenerateGetField<TOwner, TField>(string fieldName)
        {
            var param = Expression.Parameter(typeof(TOwner));
            return Expression.Lambda<Func<TOwner, TField>>(Expression.Field(param, fieldName), param).Compile();
        }

        internal static T CreateDelegate<T>(Type type, string methodName)
        {
            var args = typeof(T).GetRuntimeMethods().First(c => c.Name == nameof(Action.Invoke))
                .GetParameters().Select(p => p.ParameterType).ToArray();
            var methodInfo = type.GetRuntimeMethods().First(m => m.Name == methodName && m.GetParameters()
                .Select(p => p.ParameterType).SequenceEqual(args));
            return (T)(object)methodInfo.CreateDelegate(typeof(T));
        }
    }
}
