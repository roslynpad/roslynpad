using System;
using System.Linq;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace RoslynPad.Editor;

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
        var param = Parameter(typeof(TOwner));
        return Lambda<Func<TOwner, TField>>(Field(param, fieldName), param).Compile();
    }

    internal static TMethod CreateDelegate<TOwner, TMethod>(string methodName)
    {
        var args = typeof(TMethod).GetRuntimeMethods().First(c => c.Name == nameof(Action.Invoke))
            .GetParameters().Select(p => p.ParameterType).ToArray();
        var methodInfo = typeof(TOwner).GetRuntimeMethods().First(m => m.Name == methodName && m.GetParameters()
            .Select(p => p.ParameterType).SequenceEqual(args));
        return (TMethod)(object)methodInfo.CreateDelegate(typeof(TMethod));
    }
}
