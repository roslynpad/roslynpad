using System;
using System.Reflection;

namespace RoslynPad.Utilities
{
    internal static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object o, string name)
        {
            var type = o.GetType();
            var fieldInfo = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
            {
                throw new InvalidOperationException($"Missing field {type.FullName}.{name}");
            }
            return (T)fieldInfo.GetValue(o);
        }

        public static T GetPropertyValue<T>(this object o, string name)
        {
            var type = o.GetType();
            var propertyInfo = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
            {
                throw new InvalidOperationException($"Missing property {type.FullName}.{name}");
            }
            return (T)propertyInfo.GetValue(o);
        }
    }
}