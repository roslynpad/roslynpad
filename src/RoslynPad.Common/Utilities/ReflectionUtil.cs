using System;
using System.Linq.Expressions;

namespace RoslynPad.Utilities
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
    }
}