using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    internal static class Utilities
    {
        internal static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> function)
        {
            V value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = function(key);
                dictionary.Add(key, value);
            }

            return value;
        }

        internal static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> function)
        {
            return dictionary.GetOrAdd(key, (obj) => function());
        }

        internal static bool IsWordChar(char ch, bool verbatimIdentifierPrefixIsWordCharacter)
        {
            return char.IsLetterOrDigit(ch) || ch == '_' || (verbatimIdentifierPrefixIsWordCharacter && ch == '@');
        }
    }
}
