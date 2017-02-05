using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RoslynPad.Runtime
{
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static class ObjectExtensions
    {
        public static T Dump<T>(this T o, string header = null)
        {
            Dumped?.Invoke(o, header);
            return o;
        }

        public static T DumpAs<T, TResult>(this T o, Func<T, TResult> selector, string header = null)
        {
            selector?.Invoke(o).Dump(header);
            return o;
        }

        public static TEnumerable DumpFirst<TEnumerable>(this TEnumerable enumerable, string header = null)
            where TEnumerable : IEnumerable
            
        {
            enumerable?.Cast<object>().FirstOrDefault().Dump(header);
            return enumerable;
        }

        public static TEnumerable DumpLast<TEnumerable>(this TEnumerable enumerable, string header = null)
            where TEnumerable : IEnumerable
        {
            enumerable?.Cast<object>().LastOrDefault().Dump(header);
            return enumerable;
        }

        public static TEnumerable DumpElementAt<TEnumerable>(this TEnumerable enumerable, int index, string header = null)
            where TEnumerable : IEnumerable
        {
            enumerable?.Cast<object>().ElementAtOrDefault(index).Dump(header);
            return enumerable;
        }

        internal static event Action<object, string> Dumped;
    }
}
