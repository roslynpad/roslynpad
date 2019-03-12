using System;
using System.Collections.Generic;

namespace RoslynPad.Utilities
{
    internal static class StringExtensions
    {
        public static string Join(this IEnumerable<string> source, string separator)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (separator == null) throw new ArgumentNullException(nameof(separator));

            return string.Join(separator, source);
        }
    }
}