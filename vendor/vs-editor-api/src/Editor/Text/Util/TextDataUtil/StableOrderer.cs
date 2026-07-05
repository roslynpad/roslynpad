using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Utilities
{
    internal static class StableOrderer
    {
        private static bool OrderDependencyFunction<TValue, TMetadata>(Lazy<TValue, TMetadata> x,
            Lazy<TValue, TMetadata> y)
            where TValue : class
            where TMetadata : IOrderable
        {
            if (y.Metadata.Before?.Contains(x.Metadata.Name) == true)
            {
                return true;
            }

            if (x.Metadata.After?.Contains(y.Metadata.Name) == true)
            {
                return true;
            }

            if (x.Metadata.After?.Contains (DefaultOrderings.Lowest) == true)
            {
                return true;
            }

            return false;
        }

        public static IEnumerable<Lazy<TValue, TMetadata>> Order<TValue, TMetadata>(IEnumerable<Lazy<TValue, TMetadata>> itemsToOrder)
            where TValue : class
            where TMetadata : IOrderable
        {
            return StableTopologicalSort.Order(itemsToOrder, OrderDependencyFunction);
        }
    }
}
